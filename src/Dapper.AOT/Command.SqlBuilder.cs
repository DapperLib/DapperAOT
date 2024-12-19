#if NET6_0_OR_GREATER

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Dapper;

public readonly partial struct Command
{
    /// <summary>
    /// Create a command from an interpolated string.
    /// </summary>
    public static Command Create(ref SqlBuilder sql)
    {
        var cmd = Command.Create(sql.GetSql(), sql.Parameters);
        sql.Clear();
        return cmd;
    }

    /// <summary>
    /// Interpolated string handler intended for composing SQL.
    /// </summary>
    [InterpolatedStringHandler]
    public ref partial struct SqlBuilder(int literalLength, int formattedCount)
    {
        private Parameter[] _parameters = ArrayPool<Parameter>.Shared.Rent(formattedCount);
        private DefaultInterpolatedStringHandler _handler = new(literalLength, formattedCount, CultureInfo.InvariantCulture);
        private int _paramCount = 0;
        private bool _hasFormattedInjection = false;

        /// <summary>
        /// Gets the parameters associated with this operation
        /// </summary>
        public ReadOnlySpan<Parameter> Parameters => new(_parameters, 0, _paramCount);

        // avoid allocating composed names repeatedly
        private static readonly ConcurrentDictionary<(char token, string expression), string> _expressionCache = [];
        private static readonly ConcurrentDictionary<(char token, int index), string> _indexCache = [];

        /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendLiteral(string)"/>
        public void AppendLiteral(string value) => _handler.AppendLiteral(value);

        string ProposeAndAppendName(char token, string expression)
        {
            // for simple names, use the expression to name the parameter, so @{name} becomes @name
            // otherwise, invent, so @{id + 2} becomes @p0
            if (Regex.IsMatch(expression, "^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                _handler.AppendLiteral(expression);
                var key = (token, expression);
                if (!_expressionCache.TryGetValue(key, out var composed))
                {
                    _expressionCache[key] = composed = $"{key.token}{key.expression}";
                }
                return composed;
            }
            else
            {
                _handler.AppendLiteral("p");
                _handler.AppendFormatted(_paramCount);
                var key = (token, index: _paramCount++);
                if (!_indexCache.TryGetValue(key, out var composed))
                {
                    _indexCache[key] = composed = $"{key.token}p{key.index}";
                }
                return composed;
            }
        }

        private static partial ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler);
        private static partial void Clear(ref DefaultInterpolatedStringHandler handler);

#if NET8_0_OR_GREATER
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Text")]
        private static extern partial ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler);

        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        private static extern partial void Clear(ref DefaultInterpolatedStringHandler handler);
#else
    private static partial void Clear(ref DefaultInterpolatedStringHandler handler)
    {
        throw new NotImplementedException("TODO");
    }

    private static partial ReadOnlySpan<char> GetText(ref DefaultInterpolatedStringHandler handler)
    {
        throw new NotImplementedException("TODO");
    }

#endif

        private bool IsParameter(out char prefix)
        {
            var sql = GetText(ref _handler);
            if (!sql.IsEmpty)
            {
                prefix = sql[sql.Length - 1];
                return prefix is '@' or ':' or '$';
            }
            prefix = default;
            return false;
        }

        /// <summary>
        /// Gets the SQL represented by this instance.
        /// </summary>
        public string GetSql()
        {
            var span = GetText(ref _handler);
            if (span.IsEmpty)
            {
                return "";
            }

#if NET9_0_OR_GREATER
        // if there are no per-usage non-parameter values: use a cache of known SQL, using the alt-lookup
        if (!_hasFormattedInjection && s_UseNonFormattedCache)
        {
            if (s_nonFormattedAltCache.TryGetValue(span, out var found))
            {
                // re-usable common SQL
                return found;
            }
            else
            {
                // materialize and store it for next time
                var sql = span.ToString();
                return s_nonFormattedCache[sql] = sql;
            }
        }
#endif
            return span.ToString();
        }

#if NET9_0_OR_GREATER
    // .NET 9 has a span-based lookup that makes it practical to write a SQL cache to avoid allocating per-usage strings
    // as long as it looks like a constant-ish string (i.e. no non-parameter formatted values injected into the payload,
    // which would case cache saturation - we don't want a query with every ID in your database!)
    private static readonly ConcurrentDictionary<string, string> s_nonFormattedCache = [];
    private static readonly ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> s_nonFormattedAltCache;
    private static readonly bool s_UseNonFormattedCache = s_nonFormattedCache.TryGetAlternateLookup(out s_nonFormattedAltCache);
#endif

        /// <summary>
        /// If the preceding character indicates that this is a parameter: adds a parameter to the command;
        /// if no format is specified, appends the literal <c>0</c> or <c>1</c> to the SQL; otherwise,
        /// appends the formatted value to the SQL.
        /// </summary>
        public void AppendFormatted(bool value, int alignment = 0, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                if (IsParameter(out var prefix))
                {
                    AppendParameter(value, alignment, expression, prefix);
                }
                else
                {
                    // treat as literal 0/1
                    _handler.AppendLiteral(value ? "1" : "0");
                }
            }
            else
            {
                _hasFormattedInjection = true;
                _handler.AppendFormatted(value, alignment, format);
            }
        }

        static class TypeCache<T>
        {
            public static bool IsEnum = (Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)).IsEnum;
            public static TypeCode TypeCode = IsEnum
                ? Type.GetTypeCode(Enum.GetUnderlyingType(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)))
                : TypeCode.Object;
        }

        /// <summary>
        /// If the preceding character indicates that this is a parameter: adds a parameter to the command;
        /// otherwise, appends the formatted value to the SQL.
        /// </summary>
        public void AppendFormatted<T>(T value, int alignment = 0, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (IsParameter(out var prefix))
            {
                if (typeof(T).IsValueType && TypeCache<T>.IsEnum)
                {
                    object? raw = value;
                    if (raw is not null)
                    {
                        if ("STR".Equals(format, StringComparison.OrdinalIgnoreCase))
                        {
                            raw = raw.ToString();
                        }
                        else
                        {
                            raw = TypeCache<T>.TypeCode switch
                            {
                                TypeCode.SByte => (sbyte)raw,
                                TypeCode.Byte => (byte)raw,
                                TypeCode.Int16 => (short)raw,
                                TypeCode.UInt16 => (ushort)raw,
                                TypeCode.Int32 => (int)raw,
                                TypeCode.UInt32 => (uint)raw,
                                TypeCode.Int64 => (long)raw,
                                TypeCode.UInt64 => (ulong)raw,
                                _ => raw,
                            };
                        }
                    }
                    AppendParameter(raw, alignment, expression, prefix);
                    return;
                }
                else if (string.IsNullOrWhiteSpace(format))
                {
                    AppendParameter(value, alignment, expression, prefix);
                    return;
                }
            }

            _hasFormattedInjection = true;
            _handler.AppendFormatted(value, alignment, format);
        }

        /// <summary>
        /// If the preceding character indicates that this is a parameter: adds a parameter to the command;
        /// otherwise, appends the formatted value to the SQL.
        /// </summary>
        public void AppendFormatted(string value, int alignment = 0, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (string.IsNullOrWhiteSpace(format) && IsParameter(out var prefix))
            {
                AppendParameter(value, alignment, expression, prefix);
            }
            else
            {
                _hasFormattedInjection = true;
                _handler.AppendFormatted(value, alignment, format);
            }
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// If the preceding character indicates that this is a parameter: adds a parameter to the command;
        /// otherwise, appends the formatted value to the SQL.
        /// </summary>
        public void AppendFormatted(
#if NET8_0_OR_GREATER // _handler isn't marked 'scoped' until net8
            scoped
#endif
            ReadOnlySpan<char> value, int alignment = 0, string format = "", [CallerArgumentExpression(nameof(value))] string expression = "")
        {
            if (string.IsNullOrWhiteSpace(format) && IsParameter(out var prefix))
            {
                AppendParameter(value.ToString(), alignment, expression, prefix);
            }
            else
            {
                _hasFormattedInjection = true;
                _handler.AppendFormatted(value, alignment, format);
            }
        }
#endif

        private void AppendParameter<T>(T value, int size, string expression, char prefix)
        {
            var fullName = ProposeAndAppendName(prefix, expression);
            if (!HasParam(fullName))
            {
                if (_parameters is null or { Length: 0 })
                {
                    _parameters = ArrayPool<Parameter>.Shared.Rent(4);
                }
                else if (_parameters.Length == _paramCount)
                {
                    var bigger = ArrayPool<Parameter>.Shared.Rent(2 * _parameters.Length);
                    Parameters.CopyTo(bigger);
                    ArrayPool<Parameter>.Shared.Return(_parameters);
                    _parameters = bigger;
                }
                _parameters[_paramCount++] = new(fullName, (object?)value ?? DBNull.Value, size);
            }
        }

        private bool HasParam(string name)
        {
            foreach (var param in this.Parameters)
            {
                if (string.Equals(param.Name, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reset any data associated with this instance.
        /// </summary>
        public void Clear()
        {
            Clear(ref _handler);
            if (_parameters is not null)
            {
                ArrayPool<Parameter>.Shared.Return(_parameters);
            }
            this = default;
        }
    }
}

#endif
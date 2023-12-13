using Dapper.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

partial class TypeAccessor
{
    /// <summary>
    /// Create a <see cref="DbDataReader"/> over the provided sequence, optionally specifying the members to include.
    /// </summary>
    public static DbDataReader CreateDataReader<T>(IEnumerable<T> source, string[]? members = null, bool exact = false, [DapperAot] TypeAccessor<T>? accessor = null)
    {
        if (accessor is null) ThrowNullAccessor();
        if (source is null) ThrowNullSource();
        return new SyncAccessorDataReader<T>(source!.GetEnumerator(), accessor!, members, exact);
    }

    /// <summary>
    /// Create a <see cref="DbDataReader"/> over the provided sequence, optionally specifying the members to include.
    /// </summary>
#pragma warning disable CA1068 // CancellationToken parameters must come last
    public static DbDataReader CreateDataReader<T>(IAsyncEnumerable<T> source, string[]? members = null, bool exact = false, CancellationToken cancellationToken = default, [DapperAot] TypeAccessor<T>? accessor = null)
#pragma warning restore CA1068 // CancellationToken parameters must come last
    {
        if (accessor is null) ThrowNullAccessor();
        if (source is null) ThrowNullSource();
        return new AsyncAccessorDataReader<T>(source!.GetAsyncEnumerator(cancellationToken), accessor!, members, exact);
    }

    static void ThrowNullSource() => throw new ArgumentNullException("source");

    private static readonly Task<bool> s_CompletedTrue = Task.FromResult(true), s_CompletedFalse = Task.FromResult(false);
    internal sealed class SyncAccessorDataReader<T> : AccessorDataReader<T>
    {
        public SyncAccessorDataReader(IEnumerator<T> source, TypeAccessor<T> accessor, string[]? members, bool exact)
            : base(accessor, members, exact) => _source = source;

        private IEnumerator<T>? _source;

        public override void Close()
        {
            Current = default!;
            var tmp = _source;
            if (tmp is not null)
            {
                _source = null;
                tmp.Dispose();
            }
        }

        public override bool IsClosed => _source is null;

        public override bool Read()
        {
            if (_source?.MoveNext() == true)
            {
                Current = _source.Current;
                return true;
            }
            Close();
            return false;
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
            => Read() ? s_CompletedTrue : s_CompletedFalse;
    }

    internal sealed class AsyncAccessorDataReader<T> : AccessorDataReader<T>
    {
        public AsyncAccessorDataReader(IAsyncEnumerator<T> source, TypeAccessor<T> accessor, string[]? members, bool exact)
            : base(accessor, members, exact) => _source = source;

        private IAsyncEnumerator<T>? _source;

        public override bool Read()
        {
            if (_source is null) return false;
            var pending = _source.MoveNextAsync();
            var result = pending.IsCompletedSuccessfully ? pending.Result : pending.AsTask().Result;

            if (!result)
            {
                Close();
            }
            else
            {
                Current = _source.Current;
            }
            return result;
        }
        public override bool IsClosed => _source is null;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (_source is null) return s_CompletedFalse;
            var pending = _source.MoveNextAsync();
            if (pending.IsCompletedSuccessfully)
            {
                var result = pending.Result;
                if (!result) return CloseAsyncReturnFalse(this);
                Current = _source.Current;
                return s_CompletedTrue;
            }
            return Awaited(this, pending);

            static async Task<bool> Awaited(AsyncAccessorDataReader<T> @this, ValueTask<bool> pending)
            {
                var result = await pending;
                if (!result)
                {
                    @this.Close();
                }
                else
                {
                    @this.Current = @this._source!.Current;
                }
                return result;
            }

            static Task<bool> CloseAsyncReturnFalse(AsyncAccessorDataReader<T> @this)
            {
                var pending = @this.CloseAsync();
                return pending.IsCompletedSuccessfully() ? s_CompletedFalse : Awaited(pending);

                static async Task<bool> Awaited(Task pending)
                {
                    await pending;
                    return false;
                }
            }
        }
        public
            #if NETCOREAPP3_1_OR_GREATER 
            override
            #endif
        Task CloseAsync()
        {
            Current = default!;
            var tmp = _source;
            if (tmp is not null)
            {
                _source = null;
                var pending = tmp.DisposeAsync();

                if (pending.IsCompletedSuccessfully)
                {
                    pending.GetAwaiter().GetResult(); // satisfy IValueTaskSource requirements
                }
                else
                {
                    return pending.AsTask();
                }
            }
            return Task.CompletedTask;
        }
        public override void Close() => CloseAsync().Wait();
    }

    internal abstract class AccessorDataReader<T> : DbDataReader
    {
        private T _current;
        private readonly TypeAccessor<T> _accessor;
        private readonly int[] _tokens;
        private readonly bool _exact;

        protected T Current
        {
            get => _current;
            set => _current = value!;
        }
        public AccessorDataReader(TypeAccessor<T> accessor, string[]? members, bool exact)
        {
            _accessor = accessor;
            _exact = exact;
            _current = default!;

            int[] tokens;
            if (members is null || members.Length == 0)
            {
                _tokens = tokens = accessor.MemberCount == 0 ? [] : new int[accessor.MemberCount];
                for (int i = 0; i < tokens.Length; i++)
                {
                    tokens[i] = i;
                }
            }
            else
            {
                _tokens = tokens = new int[members.Length];
                for (int i = 0; i < _tokens.Length; i++)
                {
                    tokens[i] = GetToken(members[i]);
                }
            }
        }

        public sealed override bool IsDBNull(int ordinal) => _accessor.IsNull(_current, _tokens[ordinal]);
        public sealed override int Depth => 0;
        public sealed override int FieldCount => _tokens.Length;
        public sealed override int VisibleFieldCount => FieldCount;
        public sealed override TValue GetFieldValue<TValue>(int ordinal) => _accessor.GetValue<TValue>(_current, _tokens[ordinal]);
        public sealed override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);
        public sealed override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);
        public sealed override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);
        public sealed override DateTime GetDateTime(int ordinal) => GetFieldValue<DateTime>(ordinal);
        public sealed override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);
        public sealed override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);
        public sealed override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);
        public sealed override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);
        public sealed override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);
        public sealed override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);
        public sealed override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);
        public sealed override string GetString(int ordinal) => GetFieldValue<string>(ordinal);
        public sealed override int GetValues(object[] values)
        {
            var current = _current;
            var accessor = _accessor;
            ReadOnlySpan<int> tokens = _tokens;
            if (values.Length < tokens.Length) tokens = tokens.Slice(0, values.Length);
            int i = 0;
            foreach (var token in tokens)
            {
                values[i++] = accessor[current, token] ?? DBNull.Value;
            }
            return i;
        }

        public sealed override string GetName(int ordinal) => _accessor.GetName(_tokens[ordinal]);
        public sealed override Type GetFieldType(int ordinal)
        {
            var type = _accessor.GetType(_tokens[ordinal]);
            return Nullable.GetUnderlyingType(type) ?? type;
        }
        public sealed override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

        public sealed override bool NextResult()
        {
            Close();
            return false;
        }

        public sealed override int GetOrdinal(string name) => Array.IndexOf(_tokens, GetToken(name));
        private int GetToken(string name)
        {
            var token = _accessor.TryIndex(name, _exact);
            if (!token.HasValue) Throw(name);
            return token.GetValueOrDefault();

            static void Throw(string name) => throw new KeyNotFoundException($"Member '{name}' not found");
        }
        public sealed override object GetValue(int ordinal) => _accessor[_current, _tokens[ordinal]] ?? DBNull.Value;
        public sealed override object this[int ordinal] => _accessor[_current, _tokens[ordinal]] ?? DBNull.Value;
        public sealed override object this[string name] => _accessor[_current, GetToken(name)] ?? DBNull.Value;


        public sealed override IEnumerator GetEnumerator() => new DbEnumerator(this);

        public sealed override int RecordsAffected => 0;

        public sealed override bool HasRows => true;

        public sealed override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
            => IsDBNull(ordinal) ? s_CompletedTrue : s_CompletedFalse;

        public sealed override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();
        public sealed override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
            => throw new NotSupportedException();

        public sealed override DataTable GetSchemaTable()
        {
            // these are primarily the columns used by bulk load
            DataTable table = new()
            {
                Columns =
                {
                    {"ColumnOrdinal", typeof(int)},
                    {"ColumnName", typeof(string)},
                    {"DataType", typeof(Type)},
                    {"ColumnSize", typeof(int)},
                    {"AllowDBNull", typeof(bool)}
                }
            };
            object[] rowData = new object[5];
            var accessor = _accessor;
            int i = 0;
            foreach (var token in _tokens)
            {
                rowData[0] = i++;
                rowData[1] = accessor.GetName(token);
                rowData[2] = accessor.GetType(token);
                rowData[3] = -1;
                rowData[4] = accessor.IsNullable(token);
                table.Rows.Add(rowData);
            }
            return table;
        }
    }
}

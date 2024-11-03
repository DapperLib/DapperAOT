using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Dapper;

internal enum SomeEnum
{
    A = 1, B = 2, C = 2,
}

/// <summary>
/// Parses enum values as integers
/// </summary>
public abstract class EnumTypeHandler<T> : TypeHandler<T> where T : struct, Enum
{
    private readonly bool _writeAsString;
    internal EnumTypeHandler(bool writeAsString) => _writeAsString = writeAsString;

    private static readonly Type UnderlyingType = Enum.GetUnderlyingType(typeof(T));

    private protected static void AssertSize(int expected)
    {
        var actual = Unsafe.SizeOf<T>();
        if (actual != expected) Throw(expected, actual);
        static void Throw(int expected, int actual) => throw new InvalidOperationException($"Incorrect enum size; expected {expected}, actual {actual}");
    }

    /// <inheritdoc/>
    public sealed override int Tokenize(DbDataReader reader, int columnOffset)
    {
        var type = reader.GetFieldType(columnOffset);
        if (type == typeof(int)) return 0;
        if (type == typeof(long)) return 1;
        if (type == typeof(short)) return 2;
        if (type == typeof(sbyte)) return 3;
        if (type == typeof(string)) return 4;
        return 5;
    }

    /// <inheritdoc/>
    public sealed override T Parse(DbDataReader reader, int ordinal, int token)
        => token switch
        {
            0 => Parse(reader.GetInt32(ordinal)),
            1 => Parse(reader.GetInt64(ordinal)),
            2 => Parse(reader.GetInt16(ordinal)),
            3 => Parse(reader.GetByte(ordinal)),
            4 => Parse(reader.GetString(ordinal)),
            _ => Parse(reader.GetValue(ordinal)),
        };

    /// <inheritdoc/>
    protected override sealed void SetValueCore(DbParameter parameter, T value)
        => parameter.Value = _writeAsString ? AsString(value) : AsInteger(value);

    private protected abstract T Parse(int value);
    private protected abstract T Parse(long value);
    private protected abstract T Parse(short value);
    private protected abstract T Parse(byte value);
    private protected T Parse(string value)
    {
        if (Enum.TryParse<T>(value, true, out var result))
        {
            return result;
        }
        else if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i64))
        {
            return Parse(i64);
        }
        return Throw(value);

        static T Throw(string value)
            => throw new FormatException($"Could not parse '{value}' as {typeof(T).Name}");
    }
    private protected T Parse(object value)
    {
        var type = value.GetType();
        if (type == UnderlyingType || type == typeof(T))
        {
            // direct unbox
            return (T)value;
        }
        if (type == typeof(string))
        {
            return Parse((string)value);
        }
        // convert to the underlying integer type, then cast that
        return (T)Convert.ChangeType(value, UnderlyingType);
    }

    /// <inheritdoc/>
    public sealed override T Parse(DbParameter parameter)
    {
        var val = parameter.Value;
        if (val is null) Throw();
        return Parse(val!);

        static void Throw() => throw new InvalidOperationException("The parameter value is null; this can be checked with " + nameof(IsDBNull));
    }
    /// <summary>
    /// Gets an integer representation of the provided value
    /// </summary>
    protected abstract object AsInteger(T value);
    /// <summary>
    /// Gets a string representation of the provided value
    /// </summary>
    protected virtual object AsString(T value) => value.ToString();
}

/// <summary>
/// Parses 32-bit enum values as integers
/// </summary>
public class EnumInt32TypeHandler<T> : EnumTypeHandler<T> where T : struct, Enum
{
    /// <summary>
    /// Create a new instance
    /// </summary>
    public EnumInt32TypeHandler(bool writeAsString = false) : base(writeAsString) { }
    static EnumInt32TypeHandler() => AssertSize(sizeof(int));

    /// <inheritdoc/>
    protected sealed override void Configure(DbParameter parameter) => parameter.DbType = DbType.Int32;

    private protected sealed override T Parse(int value) => Unsafe.As<int, T>(ref value);
    private protected sealed override T Parse(long value) => Parse(checked((int)value));
    private protected sealed override T Parse(short value) => Parse((int)value);
    private protected sealed override T Parse(byte value) => Parse((int)value);

    /// <inheritdoc/>
    protected override object AsInteger(T value) =>
        CommandFactory.AsValue(Unsafe.As<T, int>(ref value));
}

file sealed class SomeEnumTypeHandler : EnumInt32TypeHandler<SomeEnum>
{
    private SomeEnumTypeHandler(bool writeAsString) : base(writeAsString) { }

    private static SomeEnumTypeHandler? s_Integer, s_String;
    public static SomeEnumTypeHandler Integer => s_Integer ??= new(false);
    public static SomeEnumTypeHandler String => s_String ??= new(true);

    // avoid allocating boxes constantly
    static readonly object s_A = (int)SomeEnum.A;
    static readonly object s_B = (int)SomeEnum.B;
    // SomeEnum.C is a duplicate of SomeEnum.B

    protected override object AsInteger(SomeEnum value) => value switch
    {
        SomeEnum.A => s_A,
        SomeEnum.B => s_B,
        // SomeEnum.C is a duplicate of SomeEnum.B
        _ => base.AsInteger(value)
    };

    protected override object AsString(SomeEnum value)
        => value switch
        {
            SomeEnum.A => nameof(SomeEnum.A),
            SomeEnum.B => nameof(SomeEnum.B),
            // SomeEnum.C is a duplicate of SomeEnum.B
            _ => base.AsString(value)
        };
}
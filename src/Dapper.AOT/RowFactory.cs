using Dapper.Internal;
using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Dapper;

/// <summary>
/// Processes rows returned from ADO.NET into typed data
/// </summary>
public abstract class RowFactory
{
    /// <summary>
    /// Provides an assortment of pre-built row factory implementations
    /// </summary>
    public static class Inbuilt
    {
        /// <summary>
        /// Private a row-factory that can dynamically parse records as <see cref="IDataRecord"/>
        /// </summary>
        public static RowFactory<IDataRecord> IDataRecord => DynamicRecordReader<IDataRecord>.Instance;

        /// <summary>
        /// Private a row-factory that can dynamically parse records as <see cref="DbDataRecord"/>
        /// </summary>
        public static RowFactory<DbDataRecord> DbDataRecord => DynamicRecordReader<DbDataRecord>.Instance;

        /// <summary>
        /// Private a row-factory that can dynamically parse records as dynamic data
        /// </summary>
        public static RowFactory<dynamic> Dynamic => DynamicRecordReader<dynamic>.Instance;

        /// <summary>
        /// Private a row-factory that can dynamically parse records as dynamic data
        /// </summary>
        public static RowFactory<object> Object => DynamicRecordReader<object>.Instance;

        /// <summary>
        /// Private a row-factory that can read simple values
        /// </summary>
        public static RowFactory<T> Value<T>() => RowFactory<T>.Default;

        /// <summary>
        /// Private a row-factory that can read simple values
        /// </summary>
        public static RowFactory<T?> NullableValue<T>() where T : struct => NullableRowFactory<T>.Default;
    }

    /// <summary>
    /// Provides a flexible typed read operation where the underlying data type
    /// is <b>not</b> required to be correct, i.e. where <see cref="DbDataReader.GetFieldValue{T}(int)"/>
    /// or methods like <see cref="DbDataReader.GetInt32(int)"/> would <b>not</b> be appropriate
    /// </summary>
    protected static T GetValue<T>(DbDataReader reader, int fieldOffset)
        => CommandUtils.As<T>(reader.GetValue(fieldOffset));

    /// <summary>
    /// Gets a value directly, using the most appropriate helper method when available (<see cref="DbDataReader.GetInt32(int)"/> etc),
    /// or <see cref="DbDataReader.GetFieldValue{T}(int)"/> otherwise.
    /// </summary>
    protected static T GetValueExact<T>(DbDataReader reader, int fieldOffset)
    {
        // note JIT elide at play here
        if (typeof(T) == typeof(int)) return UnsafeAs<int, T>(reader.GetInt32(fieldOffset));
        if (typeof(T) == typeof(short)) return UnsafeAs<short, T>(reader.GetInt16(fieldOffset));
        if (typeof(T) == typeof(long)) return UnsafeAs<long, T>(reader.GetInt64(fieldOffset));
        if (typeof(T) == typeof(byte)) return UnsafeAs<byte, T>(reader.GetByte(fieldOffset));
        if (typeof(T) == typeof(bool)) return UnsafeAs<bool, T>(reader.GetBoolean(fieldOffset));
        if (typeof(T) == typeof(DateTime)) return UnsafeAs<DateTime, T>(reader.GetDateTime(fieldOffset));
        if (typeof(T) == typeof(decimal)) return UnsafeAs<decimal, T>(reader.GetDecimal(fieldOffset));
        if (typeof(T) == typeof(double)) return UnsafeAs<double, T>(reader.GetDouble(fieldOffset));
        if (typeof(T) == typeof(float)) return UnsafeAs<float, T>(reader.GetFloat(fieldOffset));
        if (typeof(T) == typeof(Guid)) return UnsafeAs<Guid, T>(reader.GetGuid(fieldOffset));
        // do not use GetChar - it isn't reliable on all implementations
        // if (typeof(T) == typeof(char)) return UnsafeAs<char, T>(reader.GetChar(fieldOffset));
        if (typeof(T) == typeof(string)) return UnsafeAs<string, T>(reader.GetString(fieldOffset));
        return reader.GetFieldValue<T>(fieldOffset);
    }

    private static TTo UnsafeAs<TFrom, TTo>(TFrom value) => Unsafe.As<TFrom, TTo>(ref value);

    /// <summary>
    /// Provides a reliable string hashing function that ignores case, whitespace and underscores
    /// </summary>
    protected static uint NormalizedHash(string? value) => StringHashing.NormalizedHash(value);

    /// <summary>
    /// Compares a string for equality against a pre-normalized comparand, ignoring case, whitespace and underscores
    /// </summary>
    protected static bool NormalizedEquals(string? value, string? normalized) => StringHashing.NormalizedEquals(value, normalized);
}

/// <summary>
/// Processes rows returned from ADO.NET into typed data
/// </summary>
public class RowFactory<T> : RowFactory
{
    private static RowFactory<T>? _default;
    internal static RowFactory<T> Default => _default ??= new();
    /// <summary>
    /// Create a new instance
    /// </summary>
    protected RowFactory() { }
    /// <summary>
    /// Inspect the metadata of the next columns (width according to <paramref name="tokens"/>), starting
    /// from <paramref name="columnOffset"/>, allowing an opportunity to identify columns that should be mapped
    /// </summary>
    public virtual object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset)
    {
        tokens[0] = reader.GetFieldType(columnOffset) == typeof(T) ? 0 : 1;
        return null;
    }

    /// <summary>
    /// Read the data of the next columns (width according to <paramref name="tokens"/>), starting from
    /// <paramref name="columnOffset"/>, parsing the cells into a <typeparamref name="T"/> value
    /// </summary>
    public virtual T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
        => reader.IsDBNull(columnOffset) ? default! : tokens[0] == 0 ? GetValueExact<T>(reader, columnOffset) : GetValue<T>(reader, columnOffset);
}


using Dapper.Internal;
using System;
using System.Data;
using System.Data.Common;

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
        => reader.GetFieldValue<T>(fieldOffset);

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


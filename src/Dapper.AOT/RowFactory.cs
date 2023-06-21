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
    }

    /// <summary>
    /// Provides a flexible typed read operation where the underlying data type
    /// is <b>not</b> required to be correct, i.e. where <see cref="DbDataReader.GetFieldValue{T}(int)"/>
    /// or methods like <see cref="DbDataReader.GetInt32(int)"/> would <b>not</b> be appropriate
    /// </summary>
    protected static T GetValue<T>(DbDataReader reader, int fieldOffset)
        => CommandUtils.As<T>(reader.GetValue(fieldOffset));

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
    internal static readonly RowFactory<T> Default = new();
    /// <summary>
    /// Create a new instance
    /// </summary>
    protected RowFactory() { }
    /// <summary>
    /// Inspect the metadata of the next columns (width according to <paramref name="tokens"/>), starting
    /// from <paramref name="columnOffset"/>, allowing an opportunity to identify columns that should be mapped
    /// </summary>
    public virtual object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset) => null;

    /// <summary>
    /// Read the data of the next columns (width according to <paramref name="tokens"/>), starting from
    /// <paramref name="columnOffset"/>, parsing the cells into a <typeparamref name="T"/> value
    /// </summary>
    public virtual T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
        => reader.GetFieldValue<T>(columnOffset);
}


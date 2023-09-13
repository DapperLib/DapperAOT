using Dapper.Internal;
using System;
using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Dapper;

/// <summary>
/// Processes rows returned from ADO.NET into typed data
/// </summary>
public abstract class RowFactory
{
    internal static List<TRow> GetRowBuffer<TRow>(int rowCountHint) => rowCountHint <= 0 ? new() : new(rowCountHint);

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

    /// <summary>
    /// Read the current row as an <see cref="object"/>
    /// </summary>
    public abstract object ReadObject(DbDataReader reader);

    internal const int MAX_STACK_TOKENS = 64;

    internal static Span<int> Lease(int fieldCount, ref int[]? lease)
    {
        if (lease is null || lease.Length < fieldCount)
        {
            // no leased array, or existing lease is not big enough; rent a new array
            if (lease is not null) ArrayPool<int>.Shared.Return(lease);
            lease = ArrayPool<int>.Shared.Rent(fieldCount);
        }
#if NET8_0_OR_GREATER
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(lease), fieldCount);
#else
        return new Span<int>(lease, 0, fieldCount);
#endif
    }
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

    /// <inheritdoc/>
    public sealed override object ReadObject(DbDataReader reader) => Read(reader)!;

    /// <summary>
    /// Read the current row as a <typeparamref name="T"/>
    /// </summary>
    public T Read(DbDataReader reader)
    {
        int[]? lease = null;
        var result = Read(reader, ref lease);
        if (lease is not null) ArrayPool<int>.Shared.Return(lease);
        return result;
    }
    internal T Read(DbDataReader reader, ref int[]? lease)
    {
        var readWriteTokens = reader.FieldCount <= MAX_STACK_TOKENS
            ? CommandUtils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], reader.FieldCount)
            : Lease(reader.FieldCount, ref lease);

        var tokenState = Tokenize(reader, readWriteTokens, 0);
        var result = Read(reader, readWriteTokens, 0, tokenState);
        return result;
    }

    /// <summary>
    /// Reads the first row returned
    /// </summary>
    public ValueTask<T> ReadFirstAsync(DbDataReader reader, CancellationToken cancellationToken = default)
        => ReadSingleRowAsync(reader, OneRowFlags.ThrowIfNone, cancellationToken)!;

    /// <summary>
    /// Reads exactly one row
    /// </summary>
    public ValueTask<T> ReadSingleAsync(DbDataReader reader, CancellationToken cancellationToken = default)
        => ReadSingleRowAsync(reader, OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, cancellationToken)!;

    /// <summary>
    /// Reads the first row, if any are returned
    /// </summary>
    public ValueTask<T?> ReadFirstOrDefaultAsync(DbDataReader reader, CancellationToken cancellationToken = default)
        => ReadSingleRowAsync(reader, OneRowFlags.None, cancellationToken);

    /// <summary>
    /// Reads at most one row
    /// </summary>
    public ValueTask<T?> ReadSingleOrDefaultAsync(DbDataReader reader, CancellationToken cancellationToken = default)
        => ReadSingleRowAsync(reader, OneRowFlags.ThrowIfMultiple, cancellationToken);

    private async ValueTask<T?> ReadSingleRowAsync(DbDataReader reader, OneRowFlags flags, CancellationToken cancellationToken)
    {
        T? result = default;
        if (await reader.ReadAsync(cancellationToken))
        {
            result = Read(reader);

            if (await reader.ReadAsync(cancellationToken))
            {
                if ((flags & OneRowFlags.ThrowIfMultiple) != 0)
                {
                    CommandUtils.ThrowMultiple();
                }
                while (await reader.ReadAsync(cancellationToken)) { }
            }
        }
        else if ((flags & OneRowFlags.ThrowIfNone) != 0)
        {
            CommandUtils.ThrowNone();
        }
        return result;
    }
}


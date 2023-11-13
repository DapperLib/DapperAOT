using Dapper.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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


    [StructLayout(LayoutKind.Explicit, Size = 4 * sizeof(int))]
    private protected struct TokenBuffer4
    {
        [FieldOffset(0)]
        public int PayloadStart;
        [FieldOffset(0)]
        private unsafe fixed int tokens[4];
    }

    [StructLayout(LayoutKind.Explicit, Size = 8 * sizeof(int))]
    private protected struct TokenBuffer8
    {
        [FieldOffset(0)]
        public int PayloadStart;
        [FieldOffset(0)]
        private unsafe fixed int tokens[8];
    }

    [StructLayout(LayoutKind.Explicit, Size = 16 * sizeof(int))]
    private protected struct TokenBuffer16
    {
        [FieldOffset(0)]
        public int PayloadStart;
        [FieldOffset(0)]
        private unsafe fixed int tokens[16];
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

    /// <summary>
    /// Gets the row parser for a specific row on a data reader. This allows for type switching every row based on, for example, a TypeId column.
    /// You could return a collection of the base type but have each more specific.
    /// </summary>
    /// <param name="reader">The data reader to get the parser for the current row from.</param>
    /// <param name="startIndex">The start column index of the object (default: 0).</param>
    /// <param name="length">The length of columns to read (default: -1 = all fields following startIndex).</param>
    /// <param name="returnNullIfFirstMissing">Return null if the value of the first column is <c>null</c>.</param>
    /// <returns>A parser for this specific object from this row.</returns>
    public Func<DbDataReader, T> GetRowParser(DbDataReader reader,
            int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
    {
        if (length < 0) // use everything remaining
        {
            length = reader.FieldCount - startIndex;
        }
        if (length == 0)
        {
            returnNullIfFirstMissing = false; // no "first" to ask about!
        }
        RowParserState state = length switch
        {
            0 => new EmptyRowParserState(this, startIndex, length, returnNullIfFirstMissing),
#if NETCOREAPP3_1_OR_GREATER
            <= 4 => new FixedSizeRowParserState4(this, startIndex, length, returnNullIfFirstMissing),
            <= 8 => new FixedSizeRowParserState8(this, startIndex, length, returnNullIfFirstMissing),
            <= 16 => new FixedSizeRowParserState16(this, startIndex, length, returnNullIfFirstMissing),
#endif
            _ => new LargeRowParserState(this, startIndex, length, returnNullIfFirstMissing),
        };
        state.Tokenize(reader);
        return state.Parse;
    }

    private abstract class RowParserState
    {
        // manual capture state for row reader; initialized with tokenized column metadata, then
        // reused between different rows
        protected RowParserState(RowFactory<T> rowFactory, int columnOffset, bool returnNullIfFirstMissing)
        {
            this.rowFactory = rowFactory;
            if (columnOffset < 0) Throw();
            columnOffsetAndReturnNullIfFirstMissing = columnOffset | (returnNullIfFirstMissing ? MSB : 0);

            static void Throw() => throw new ArgumentOutOfRangeException(nameof(columnOffset));
        }

        const int MSB = 1 << 31;

        private int ColumnOffset => columnOffsetAndReturnNullIfFirstMissing & ~MSB;
        private bool ReturnNullIfFirstMissing => (columnOffsetAndReturnNullIfFirstMissing & MSB) != 0;

        private readonly RowFactory<T> rowFactory;
        private readonly int columnOffsetAndReturnNullIfFirstMissing;
        private object? state;
        protected abstract Span<int> Tokens { get; }

        protected abstract ReadOnlySpan<int> ReadOnlyTokens { get; }
        public void Tokenize(DbDataReader reader)
            => state = rowFactory.Tokenize(reader, Tokens, ColumnOffset);

        protected static void ValidateLength(int length, int max)
        {
            if (length < 0 || length > max) Throw();
            static void Throw() => throw new ArgumentOutOfRangeException(nameof(length));
        }

        public T Parse(DbDataReader reader)
        {
            if (ReturnNullIfFirstMissing && reader.IsDBNull(ColumnOffset))
            {
                return default!;
            }
            return rowFactory.Read(reader, ReadOnlyTokens, ColumnOffset, state);
        }
    }

    private sealed class EmptyRowParserState : RowParserState
    {
        public EmptyRowParserState(RowFactory<T> rowFactory, int columnOffset, int length, bool returnNullIfFirstMissing)
            : base(rowFactory, columnOffset, returnNullIfFirstMissing)
        {
            ValidateLength(length, 0);
        }

        protected override Span<int> Tokens => default;
        protected override ReadOnlySpan<int> ReadOnlyTokens => default;
    }

#if NETCOREAPP3_1_OR_GREATER // can optimize using spans over a payload struct
    private sealed class FixedSizeRowParserState4 : RowParserState
    {
        private readonly int length;
        private TokenBuffer4 tokenBuffer;
        public FixedSizeRowParserState4(RowFactory<T> rowFactory, int columnOffset, int length, bool returnNullIfFirstMissing)
            : base(rowFactory, columnOffset, returnNullIfFirstMissing)
        {
            ValidateLength(length, 4);
            this.length = length;
        }
        protected override Span<int> Tokens => MemoryMarshal.CreateSpan(ref tokenBuffer.PayloadStart, length);
        protected override ReadOnlySpan<int> ReadOnlyTokens => MemoryMarshal.CreateReadOnlySpan(ref tokenBuffer.PayloadStart, length);
    }
    private sealed class FixedSizeRowParserState8 : RowParserState
    {
        private readonly int length;
        private TokenBuffer8 tokenBuffer;
        public FixedSizeRowParserState8(RowFactory<T> rowFactory, int columnOffset, int length, bool returnNullIfFirstMissing)
            : base(rowFactory, columnOffset, returnNullIfFirstMissing)
        {
            ValidateLength(length, 8);
            this.length = length;
        }
        protected override Span<int> Tokens => MemoryMarshal.CreateSpan(ref tokenBuffer.PayloadStart, length);
        protected override ReadOnlySpan<int> ReadOnlyTokens => MemoryMarshal.CreateReadOnlySpan(ref tokenBuffer.PayloadStart, length);
    }
    private sealed class FixedSizeRowParserState16 : RowParserState
    {
        private readonly int length;
        private TokenBuffer16 tokenBuffer;
        public FixedSizeRowParserState16(RowFactory<T> rowFactory, int columnOffset, int length, bool returnNullIfFirstMissing)
            : base(rowFactory, columnOffset, returnNullIfFirstMissing)
        {
            ValidateLength(length, 16);
            this.length = length;
        }
        protected override Span<int> Tokens => MemoryMarshal.CreateSpan(ref tokenBuffer.PayloadStart, length);
        protected override ReadOnlySpan<int> ReadOnlyTokens => MemoryMarshal.CreateReadOnlySpan(ref tokenBuffer.PayloadStart, length);
    }
#endif

    private sealed class LargeRowParserState : RowParserState // fallback to arrays if nothing else possible
    {
        private readonly int[] tokens;
        public LargeRowParserState(RowFactory<T> rowFactory, int columnOffset, int length, bool returnNullIfFirstMissing)
            : base(rowFactory, columnOffset, returnNullIfFirstMissing)
        {
            ValidateLength(length, 0X7FEFFFFF);
            tokens = new int[length];
        }
        protected override Span<int> Tokens => new(tokens);
        protected override ReadOnlySpan<int> ReadOnlyTokens => new(tokens);
    }
}


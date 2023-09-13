// DO NOT add using directives; this file is written directly into output, and we don't want *any* ambiguity

namespace Dapper
{
    // these are DapperAOT library extensions that we can't implement this in DapperAOT itself because we don't have
    // the library reference to Dapper (since we don't know whether the user is using "Dapper" vs "Dapper.StrongName")

#if !DAPPERAOT_INTERNAL // used in integration tests
    file
#endif
    sealed class AotWrappedDbDataReader : global::Dapper.WrappedDbDataReader, global::Dapper.IWrappedDataReader
    {   // provides IWrappedDataReader support for WrappedDbDataReader, used via QueryMultiple and ExecuteReader
        global::System.Data.IDataReader global::Dapper.IWrappedDataReader.Reader => Reader;
        global::System.Data.IDbCommand global::Dapper.IWrappedDataReader.Command => Command;
    }

#if !DAPPERAOT_INTERNAL // used in integration tests
    file
#endif
    sealed class AotGridReader : global::Dapper.SqlMapper.GridReader
    {   // provides GridReader support, used via QueryMultiple
        public AotGridReader(
            global::System.Data.Common.DbDataReader reader,
            global::System.Threading.CancellationToken cancellationToken = default)
            : base(reader is IWrappedDataReader wrapped ? wrapped.Command : null!, reader, null!, null, null, false, cancellationToken)
        {}

        public T ReadSingle<T>(RowFactory<T> rowFactory) => ReadSingleRow(rowFactory, OneRowFlags.ThrowIfMultiple | OneRowFlags.ThrowIfNone)!;
        
        public T? ReadSingleOrDefault<T>(RowFactory<T> rowFactory) => ReadSingleRow(rowFactory, OneRowFlags.ThrowIfMultiple);

        public T ReadFirst<T>(RowFactory<T> rowFactory) => ReadSingleRow(rowFactory, OneRowFlags.ThrowIfNone)!;
        public T? ReadFirstOrDefault<T>(RowFactory<T> rowFactory) => ReadSingleRow(rowFactory, OneRowFlags.None)!;

        public global::System.Threading.Tasks.ValueTask<T> ReadSingleAsync<T>(RowFactory<T> rowFactory) => ReadSingleRowAsync(rowFactory, OneRowFlags.ThrowIfMultiple | OneRowFlags.ThrowIfNone)!;

        public global::System.Threading.Tasks.ValueTask<T?> ReadSingleOrDefaultAsync<T>(RowFactory<T> rowFactory) => ReadSingleRowAsync(rowFactory, OneRowFlags.ThrowIfMultiple);

        public global::System.Threading.Tasks.ValueTask<T> ReadFirstAsync<T>(RowFactory<T> rowFactory) => ReadSingleRowAsync(rowFactory, OneRowFlags.ThrowIfNone)!;
        public global::System.Threading.Tasks.ValueTask<T?> ReadFirstOrDefaultAsync<T>(RowFactory<T> rowFactory) => ReadSingleRowAsync(rowFactory, OneRowFlags.None)!;

        public global::System.Collections.Generic.IEnumerable<T> Read<T>(bool buffered, RowFactory<T> rowFactory)
            => buffered ? ReadBuffered(rowFactory) : ReadUnbuffered(rowFactory);

        private const int MAX_STACK_TOKENS = 64;

        private static global::System.Collections.Generic.List<TRow> GetRowBuffer<TRow>(int rowCountHint) => rowCountHint <= 0 ? new() : new(rowCountHint); // can't use RowFactory because of location

        public global::System.Collections.Generic.List<T> ReadBuffered<T>(RowFactory<T> rowFactory, int rowCountHint = 0)
        {
            var index = OnBeforeGrid();
            int[]? lease = null;
            try
            {
                var results = GetRowBuffer<T>(rowCountHint);
                var reader = Reader;
                if (reader.Read() && index == ResultIndex)
                {
                    var readWriteTokens = reader.FieldCount <= MAX_STACK_TOKENS
                        ? UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], reader.FieldCount)
                        : Lease(reader.FieldCount, ref lease);

                    var tokenState = rowFactory.Tokenize(reader, readWriteTokens, 0);
                    global::System.ReadOnlySpan<int> readOnlyTokens = readWriteTokens; // avoid multiple conversions
                    do
                    {
                        results.Add(rowFactory.Read(reader, readOnlyTokens, 0, tokenState));
                    }
                    while (reader.Read() && index == ResultIndex);
                }
                return results;
            }
            finally
            {
                if (lease is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(lease);
                OnAfterGrid(index);
            }

            static global::System.Span<int> UnsafeSlice(global::System.Span<int> value, int length)
            {
                global::System.Diagnostics.Debug.Assert(length >= 0 && length <= value.Length);
#if NETCOREAPP3_1_OR_GREATER
                return global::System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref global::System.Runtime.InteropServices.MemoryMarshal.GetReference(value), length);
#else
                return value.Slice(0, length);
#endif
            }
        }

        public global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> ReadAsync<T>(bool buffered, RowFactory<T> rowFactory)
            => buffered ? ReadBufferedAsync(rowFactory, rowCountHint: 0) : ReadFalseUnbufferedAsync(rowFactory);

        private async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> ReadBufferedAsync<T>(RowFactory<T> rowFactory, int rowCountHint)
        {
            var index = OnBeforeGrid();
            int[]? lease = null;
            try
            {
                var results = GetRowBuffer<T>(rowCountHint);
                var reader = Reader;
                if (await reader.ReadAsync(CancellationToken) && index == ResultIndex)
                {
                    var tokenState = rowFactory.Tokenize(reader, Lease(reader.FieldCount, ref lease), 0);
                    do
                    {
                        results.Add(rowFactory.Read(reader, Tokens(lease, reader.FieldCount), 0, tokenState));
                    }
                    while (await reader.ReadAsync(CancellationToken) && index == ResultIndex);
                }
                return results;
            }
            finally
            {
                if (lease is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(lease);
                OnAfterGrid(index);
            }
        }

        private static global::System.ReadOnlySpan<int> Tokens(int[]? lease, int fieldCount)
                => new global::System.ReadOnlySpan<int>(lease, 0, fieldCount);

        private global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<T>> ReadFalseUnbufferedAsync<T>(RowFactory<T> rowFactory)
        {   // this is not great, but is consistent with Dapper behavior
            return System.Threading.Tasks.Task.FromResult(ReadUnbuffered<T>(rowFactory));
        }

        public global::System.Collections.Generic.IAsyncEnumerable<T> ReadUnbufferedAsync<T>(RowFactory<T> rowFactory)
            => ReadTrueUnbufferedAsync<T>(rowFactory, CancellationToken);

        private async global::System.Collections.Generic.IAsyncEnumerable<T> ReadTrueUnbufferedAsync<T>(RowFactory<T> rowFactory,
            [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken)
        {
            var index = OnBeforeGrid();
            int[]? lease = null;
            try
            {
                var reader = Reader;
                if (await reader.ReadAsync(cancellationToken) && index == ResultIndex)
                {
                    var tokenState = rowFactory.Tokenize(reader, Lease(reader.FieldCount, ref lease), 0);
                    do
                    {
                        yield return rowFactory.Read(reader, Tokens(lease, reader.FieldCount), 0, tokenState);
                    }
                    while (await reader.ReadAsync(cancellationToken) && index == ResultIndex);
                }
            }
            finally
            {
                if (lease is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(lease);
                OnAfterGrid(index);
            }
        }

        private static global::System.Span<int> Lease(int fieldCount, ref int[]? lease)
        {
            if (lease is null || lease.Length < fieldCount)
            {
                // no leased array, or existing lease is not big enough; rent a new array
                if (lease is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(lease);
                lease = global::System.Buffers.ArrayPool<int>.Shared.Rent(fieldCount);
            }
#if NET8_0_OR_GREATER
            return global::System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref global::System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(lease), fieldCount);
#else
            return new global::System.Span<int>(lease, 0, fieldCount);
#endif
        }

        public global::System.Collections.Generic.IEnumerable<T> ReadUnbuffered<T>(RowFactory<T> rowFactory)
        {
            var index = OnBeforeGrid();
            int[]? lease = null;
            try
            {
                var reader = Reader;
                if (reader.Read() && index == ResultIndex)
                {
                    int fieldCount = reader.FieldCount;
                    var tokenState = rowFactory.Tokenize(reader, Lease(fieldCount, ref lease), 0);
                    do
                    {
                        yield return rowFactory.Read(reader, GetTokenSpan(lease!, fieldCount), 0, tokenState);
                    }
                    while (reader.Read() && index == ResultIndex);
                }
            }
            finally
            {
                if (lease is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(lease);
                OnAfterGrid(index);
            }
        }

        static global::System.ReadOnlySpan<int> GetTokenSpan(int[] lease, int fieldCount)
        {
#if NET8_0_OR_GREATER
            return global::System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref global::System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(lease), fieldCount);
#else
            return new global::System.Span<int>(lease, 0, fieldCount);
#endif
        }



        [global::System.Flags]
        internal enum OneRowFlags
        {
            None = 0,
            ThrowIfNone = 1 << 0,
            ThrowIfMultiple = 1 << 1,
        }

        private T? ReadSingleRow<T>(RowFactory<T> rowFactory, OneRowFlags flags)
        {
            var index = OnBeforeGrid();
            try
            {
                T? result = default;
                var reader = Reader;
                if (reader.Read() && index == ResultIndex)
                {
                    result = rowFactory.Read(reader);

                    if (reader.Read() && index == ResultIndex)
                    {
                        if ((flags & OneRowFlags.ThrowIfMultiple) != 0)
                        {
                            ThrowMultiple();
                        }
                        while (reader.Read() && index == ResultIndex) { }
                    }
                }
                else if ((flags & OneRowFlags.ThrowIfNone) != 0)
                {
                    ThrowNone();
                }
                return result;
            }
            finally
            {
                OnAfterGrid(index);
            }
        }
        static void ThrowNone() => _ = global::System.Linq.Enumerable.First("");
        static void ThrowMultiple() => _ = global::System.Linq.Enumerable.Single("  ");

        private async global::System.Threading.Tasks.ValueTask<T?> ReadSingleRowAsync<T>(RowFactory<T> rowFactory, OneRowFlags flags)
        {
            var index = OnBeforeGrid();
            try
            {
                T? result = default;
                var reader = Reader;
                if (await reader.ReadAsync(CancellationToken) && index == ResultIndex)
                {
                    result = rowFactory.Read(reader);

                    if (await reader.ReadAsync(CancellationToken) && index == ResultIndex)
                    {
                        if ((flags & OneRowFlags.ThrowIfMultiple) != 0)
                        {
                            ThrowMultiple();
                        }
                        while (await reader.ReadAsync(CancellationToken) && index == ResultIndex) { }
                    }
                }
                else if ((flags & OneRowFlags.ThrowIfNone) != 0)
                {
                    ThrowNone();
                }
                return result;
            }
            finally
            {
                OnAfterGrid(index);
            }
        }
    }
}

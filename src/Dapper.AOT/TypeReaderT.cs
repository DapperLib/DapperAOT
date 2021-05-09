using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    /// <inheritdoc />
    public abstract class TypeReader<T> : TypeReader
    {
        /// <inheritdoc />
        public sealed override Type Type => typeof(T);
        /// <inheritdoc />
        public sealed override object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens, int offset = 0)
            => Read(reader, tokens, offset)!;
        /// <inheritdoc />
        public sealed override object ReadObject(IDataReader reader, ReadOnlySpan<int> tokens, int offset = 0)
            => reader is DbDataReader db ? Read(db, tokens, offset)! : ReadFallback(reader, tokens, offset)!;
        /// <inheritdoc />
        public sealed override ValueTask<object> ReadObjectAsync(DbDataReader reader, ArraySegment<int> tokens, CancellationToken cancellationToken)
        {
            var pending = ReadAsync(reader, tokens, cancellationToken);
            return pending.IsCompletedSuccessfully ? new ValueTask<object>(result: pending.Result!) : Awaited(pending);

            static async ValueTask<object> Awaited(ValueTask<T> pending) => (await pending.ConfigureAwait(false))!;
        }

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read(IDataReader reader, ReadOnlySpan<int> tokens, int offset = 0)
            => reader is DbDataReader db ? Read(db, tokens, offset) : ReadFallback(reader, tokens, offset);

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public virtual T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int offset = 0)
            => ReadFallback(reader, tokens, offset); // we'd normally expect implementors to provide this as an optimization, note

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        protected abstract T ReadFallback(IDataReader reader, ReadOnlySpan<int> tokens, int offset);

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract ValueTask<T> ReadAsync(DbDataReader reader, ArraySegment<int> tokens, CancellationToken cancellationToken);

        /// <summary>
        /// Read a single element from the source
        /// </summary>
        public T Read(DbDataReader reader, ref int[]? buffer)
        {
            Span<int> tokens = reader.FieldCount <= MaxStackTokens ? stackalloc int[reader.FieldCount] : RentSpan(ref buffer, reader.FieldCount);
            IdentifyFieldTokensFromData(reader, tokens, 0);
            return Read(reader, tokens);
        }

        /// <summary>
        /// Read a single element from the source
        /// </summary>
        public T Read(IDataReader reader, ref int[]? buffer)
            => reader is DbDataReader db ? Read(db, ref buffer) : ReadFallback(reader, ref buffer);

        private T ReadFallback(IDataReader reader, ref int[]? buffer)
        {
            Span<int> tokens = reader.FieldCount <= MaxStackTokens ? stackalloc int[reader.FieldCount] : RentSpan(ref buffer, reader.FieldCount);
            IdentifyFieldTokensFromDataFallback(reader, tokens, 0);
            return ReadFallback(reader, tokens, 0);
        }

        /// <summary>
        /// Read a single element from the source
        /// </summary>
        public ValueTask<T> ReadAsync(DbDataReader reader, ref int[]? buffer, CancellationToken cancellationToken)
        {
            var tokens = RentSegment(ref buffer, reader.FieldCount);
            IdentifyFieldTokensFromData(reader, tokens, 0);
            return ReadAsync(reader, tokens, cancellationToken);
        }
    }
}

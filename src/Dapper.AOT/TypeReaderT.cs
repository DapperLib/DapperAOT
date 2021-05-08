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
        public sealed override object ReadObject(DbDataReader reader, ReadOnlySpan<int> tokens)
            => Read(reader, tokens)!;
        /// <inheritdoc />
        public sealed override object ReadObject(IDataReader reader, ReadOnlySpan<int> tokens)
            => reader is DbDataReader db ? Read(db, tokens)! : ReadFallback(reader, tokens)!;
        /// <inheritdoc />
        public sealed override ValueTask<object> ReadObjectAsync(DbDataReader reader, ReadOnlySpan<int> tokens, CancellationToken cancellationToken)
        {
            var pending = ReadAsync(reader, tokens, cancellationToken);
            return pending.IsCompletedSuccessfully ? new ValueTask<object>(result: pending.Result!) : Awaited(pending);

            static async ValueTask<object> Awaited(ValueTask<T> pending) => (await pending.ConfigureAwait(false))!;
        }

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read(IDataReader reader, ReadOnlySpan<int> tokens)
            => reader is DbDataReader db ? Read(db, tokens) : ReadFallback(reader, tokens);

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public virtual T Read(DbDataReader reader, ReadOnlySpan<int> tokens)
            => ReadFallback(reader, tokens); // we'd normally expect implementors to provide this as an optimization, note

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        protected abstract T ReadFallback(IDataReader reader, ReadOnlySpan<int> tokens);

        /// <summary>
        /// Read a row from the supplied reader, using the tokens previously nominated by the handler
        /// </summary>
        public abstract ValueTask<T> ReadAsync(DbDataReader reader, ReadOnlySpan<int> tokens, CancellationToken cancellationToken);
    }
}

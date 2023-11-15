using Dapper.Internal;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper
{
    /// <summary>
    /// Encapsulates a <see cref="DbDataReader"/> inside another, allowing for command post-processing etc
    /// </summary>
    public class WrappedDbDataReader : DbDataReader
    {
        internal static readonly Task<bool> TaskTrue = Task.FromResult(true), TaskFalse = Task.FromResult(false);
        private IQueryState? state;
        private CommandFactory commandFactory = null!;
        private object? args;

        /// <inheritdoc/>
        public WrappedDbDataReader() { }

        internal void Initialize(CommandFactory commandFactory, object? args, ref AsyncQueryState? state)
        {
            this.commandFactory = commandFactory;
            this.args = args;
            this.state = state;
            state = default; // we've assumed ownership
        }

        internal void Initialize(CommandFactory commandFactory, object? args, ref SyncQueryState state)
        {
            this.commandFactory = commandFactory;
            this.args = args;
            this.state = state;
            state = default; // we've assumed ownership
        }

        /// <inheritdoc/>
        public sealed override bool IsClosed => state?.Reader is null or { IsClosed: true };

        /// <inheritdoc/>
        public sealed override void Close()
        {
            if (IsClosed) return;
            var state = this.state; // snapshot
            if (state is not null)
            {
                commandFactory.PostProcessObject(new(state.Command!), args, state.Reader.CloseAndCapture());
                if (commandFactory.TryRecycle(state.Command!))
                {
                    state.Command = null;
                }
                state.Reader?.Close();
                state.Dispose();
            }
            base.Close();
        }

        /// <summary>
        /// Gets the underlying <see cref="DbDataReader"/> behind this instance
        /// </summary>
        protected DbDataReader Reader
        {
            get
            {
                return state?.Reader ?? ThrowDisposed();
                static DbDataReader ThrowDisposed() => throw new ObjectDisposedException(nameof(WrappedDbDataReader));
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="DbCommand"/> behind this instance
        /// </summary>
        protected DbCommand Command => state?.Command!;

        /// <inheritdoc/>
        public sealed override bool NextResult()
        {
            var result = Reader.NextResult();
            if (!result) Close();
            return result;
        }

        /// <inheritdoc/>
        public sealed override bool Read() => Reader.Read();
        /// <inheritdoc/>
        public sealed override int Depth => Reader.Depth;
        /// <inheritdoc/>
        public sealed override int FieldCount => Reader.FieldCount;

        /// <inheritdoc/>
        public sealed override Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            var nrPending = Reader.NextResultAsync(cancellationToken);
            if (!nrPending.IsCompletedSuccessfully())
            {
                return Awaited(this, nrPending);

                static async Task<bool> Awaited(WrappedDbDataReader @this, Task<bool> nrPending)
                {
                    var result = await nrPending;
#if NETCOREAPP3_1_OR_GREATER
                    if (!result) await @this.CloseAsync();
#else
                    if (!result) @this.Close();
#endif
                    return result;
                }
            }
            var result = nrPending.Result;
            if (!result)
            {
#if NETCOREAPP3_1_OR_GREATER
                var cPending = CloseAsync();
                if (!cPending.IsCompletedSuccessfully())
                {
                    return AwaitClose(cPending);

                    static async Task<bool> AwaitClose(Task cPending)
                    {
                        await cPending;
                        return false;
                    }
                }
#else
                Close();
#endif
            }
            return result ? TaskTrue : TaskFalse;
        }
        /// <inheritdoc/>
        public sealed override Task<bool> ReadAsync(CancellationToken cancellationToken) => Reader.ReadAsync(cancellationToken);
        /// <inheritdoc/>
        public sealed override bool GetBoolean(int ordinal) => Reader.GetBoolean(ordinal);
        /// <inheritdoc/>
        public sealed override byte GetByte(int ordinal) => Reader.GetByte(ordinal);
        /// <inheritdoc/>
        public sealed override char GetChar(int ordinal) => Reader.GetChar(ordinal);
        /// <inheritdoc/>
        public sealed override string GetDataTypeName(int ordinal) => Reader.GetDataTypeName(ordinal);
        /// <inheritdoc/>
        public sealed override DateTime GetDateTime(int ordinal) => Reader.GetDateTime(ordinal);
        /// <inheritdoc/>
        public sealed override decimal GetDecimal(int ordinal) => Reader.GetDecimal(ordinal);
        /// <inheritdoc/>
        public sealed override double GetDouble(int ordinal) => Reader.GetDouble(ordinal);
        /// <inheritdoc/>
        public sealed override Type GetFieldType(int ordinal) => Reader.GetFieldType(ordinal);
        /// <inheritdoc/>
        public sealed override float GetFloat(int ordinal) => Reader.GetFloat(ordinal);
        /// <inheritdoc/>
        public sealed override Guid GetGuid(int ordinal) => Reader.GetGuid(ordinal);
        /// <inheritdoc/>
        public sealed override short GetInt16(int ordinal) => Reader.GetInt16(ordinal);
        /// <inheritdoc/>
        public sealed override int GetInt32(int ordinal) => Reader.GetInt32(ordinal);
        /// <inheritdoc/>
        public sealed override long GetInt64(int ordinal) => Reader.GetInt64(ordinal);
        /// <inheritdoc/>
        public sealed override string GetName(int ordinal) => Reader.GetName(ordinal);
        /// <inheritdoc/>
        public sealed override string GetString(int ordinal) => Reader.GetString(ordinal);
        /// <inheritdoc/>
        public sealed override object GetValue(int ordinal) => Reader.GetValue(ordinal);
        /// <inheritdoc/>
        public sealed override int GetValues(object[] values) => Reader.GetValues(values);
        /// <inheritdoc/>
        public sealed override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => Reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        /// <inheritdoc/>
        public sealed override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => Reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        /// <inheritdoc/>
        public sealed override IEnumerator GetEnumerator() => new DbEnumerator(this);
        /// <inheritdoc/>
        public sealed override int GetOrdinal(string name) => Reader.GetOrdinal(name);
        /// <inheritdoc/>
        public sealed override bool HasRows => Reader.HasRows;
        /// <inheritdoc/>
        public sealed override bool IsDBNull(int ordinal) => Reader.IsDBNull(ordinal);
        /// <inheritdoc/>
        public sealed override int RecordsAffected => Reader.RecordsAffected;
        /// <inheritdoc/>
        public sealed override object this[int ordinal] => Reader[ordinal];
        /// <inheritdoc/>
        public sealed override object this[string name] => Reader[name];
        /// <inheritdoc/>
        public sealed override DataTable? GetSchemaTable() => Reader.GetSchemaTable();
        /// <inheritdoc/>
        protected sealed override DbDataReader GetDbDataReader(int ordinal) => throw new NotSupportedException();
        /// <inheritdoc/>
        public sealed override T GetFieldValue<T>(int ordinal) => Reader.GetFieldValue<T>(ordinal);
        /// <inheritdoc/>
        public sealed override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => Reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        /// <inheritdoc/>
        public sealed override Type GetProviderSpecificFieldType(int ordinal) => Reader.GetProviderSpecificFieldType(ordinal);
        /// <inheritdoc/>
        public sealed override object GetProviderSpecificValue(int ordinal) => Reader.GetProviderSpecificValue(ordinal);
        /// <inheritdoc/>
        public sealed override int GetProviderSpecificValues(object[] values) => Reader.GetProviderSpecificValues(values);
        /// <inheritdoc/>
        public sealed override TextReader GetTextReader(int ordinal) => Reader.GetTextReader(ordinal);
        /// <inheritdoc/>
        public sealed override Stream GetStream(int ordinal) => Reader.GetStream(ordinal);
        /// <inheritdoc/>
        public sealed override int VisibleFieldCount => Reader.VisibleFieldCount;
        /// <inheritdoc/>
        public sealed override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => Reader.IsDBNullAsync(ordinal, cancellationToken);
        /// <inheritdoc/>
#if NET5_0_OR_GREATER
        [Obsolete("This API is for remoting purposes and is no longer supported")]
#endif
        public sealed override object InitializeLifetimeService() => Reader.InitializeLifetimeService();

#if NET6_0_OR_GREATER
        /// <inheritdoc/>
        public sealed override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default) => Reader.GetColumnSchemaAsync(cancellationToken);
        /// <inheritdoc/>
        public sealed override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default) => Reader.GetSchemaTableAsync(cancellationToken);
#endif

#if NETCOREAPP3_1_OR_GREATER
        /// <inheritdoc/>
        public sealed override Task CloseAsync() => IsClosed ? Task.CompletedTask : CloseAsyncImpl();
        private async Task CloseAsyncImpl()
        {
            var state = this.state; // snapshot
            if (state is not null)
            {
                commandFactory.PostProcessObject(new(state.Command!), args, await state.Reader.CloseAndCaptureAsync());
                if (commandFactory.TryRecycle(state.Command!))
                {
                    state.Command = null;
                }
                var reader = state.Reader;
                if (reader is not null) await reader.CloseAsync();
                await state.DisposeAsync();
            }
            await base.CloseAsync();
        }
#endif
    }
}

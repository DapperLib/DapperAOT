using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal
{
    internal abstract class WrappedReader : DbDataReader
    {
        protected static readonly Task<bool> TaskTrue = Task.FromResult(true), TaskFalse = Task.FromResult(false);
    }
    internal sealed class WrappedReader<TArgs> : WrappedReader
    {
        private QueryState state;
        private readonly CommandFactory<TArgs> commandFactory;
        private readonly TArgs args;

        public WrappedReader(CommandFactory<TArgs> commandFactory, TArgs args, ref QueryState state)
        {
            this.commandFactory = commandFactory;
            this.args = args;
            this.state = state;
            state = default; // we've assumed ownership
        }

        public override bool IsClosed => state.Reader is null or { IsClosed: true };

        public override void Close()
        {
            if (IsClosed) return;
            commandFactory.PostProcess(state.Command!, args);
            if (commandFactory.TryRecycle(state.Command!))
            {
                state.Command = null;
            }
            state.Reader!.Close();
            state.Dispose();
            base.Close();
        }

        private DbDataReader Reader
        {
            get
            {
                return state.Reader ?? ThrowDisposed();
                static DbDataReader ThrowDisposed() => throw new ObjectDisposedException(nameof(WrappedReader<TArgs>));
            }
        }

        public override bool NextResult()
        {
            var result = Reader.NextResult();
            if (!result) Close();
            return result;
        }

        public override bool Read() => Reader.Read();

        public override int Depth => Reader.Depth;

        public override int FieldCount => Reader.FieldCount;

        public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            var nrPending = Reader.NextResultAsync(cancellationToken);
            if (!nrPending.IsCompletedSuccessfully())
            {
                return Awaited(this, nrPending);

                static async Task<bool> Awaited(WrappedReader<TArgs> @this, Task<bool> nrPending)
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
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Reader.ReadAsync(cancellationToken);

        public override bool GetBoolean(int ordinal) => Reader.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => Reader.GetByte(ordinal);
        public override char  GetChar(int ordinal) => Reader.GetChar(ordinal);
        public override string GetDataTypeName(int ordinal) => Reader.GetDataTypeName(ordinal);
        public override DateTime GetDateTime(int ordinal) => Reader.GetDateTime(ordinal);
        public override decimal GetDecimal(int ordinal) => Reader.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => Reader.GetDouble(ordinal);
        public override Type GetFieldType(int ordinal) => Reader.GetFieldType(ordinal);
        public override float GetFloat(int ordinal) => Reader.GetFloat(ordinal);
        public override Guid GetGuid(int ordinal) => Reader.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => Reader.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => Reader.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => Reader.GetInt64(ordinal);
        public override string GetName(int ordinal) => Reader.GetName(ordinal);
        public override string GetString(int ordinal) => Reader.GetString(ordinal);
        public override object GetValue(int ordinal) => Reader.GetValue(ordinal);
        public override int GetValues(object[] values) => Reader.GetValues(values);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => Reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => Reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override IEnumerator GetEnumerator() => new DbEnumerator(this);
        public override int GetOrdinal(string name) => Reader.GetOrdinal(name);
        public override bool HasRows => Reader.HasRows;
        public override bool IsDBNull(int ordinal) => Reader.IsDBNull(ordinal);
        public override int RecordsAffected => Reader.RecordsAffected;
        public override object this[int ordinal] => Reader[ordinal];
        public override object this[string name] => Reader[name];
        public override DataTable? GetSchemaTable() => Reader.GetSchemaTable();
        protected override DbDataReader GetDbDataReader(int ordinal) => throw new NotSupportedException();
        public override T GetFieldValue<T>(int ordinal) => Reader.GetFieldValue<T>(ordinal);
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => Reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        public override Type GetProviderSpecificFieldType(int ordinal) => Reader.GetProviderSpecificFieldType(ordinal);
        public override object GetProviderSpecificValue(int ordinal) => Reader.GetProviderSpecificValue(ordinal);
        public override int GetProviderSpecificValues(object[] values) => Reader.GetProviderSpecificValues(values);
        public override TextReader GetTextReader(int ordinal) => Reader.GetTextReader(ordinal);
        public override Stream GetStream(int ordinal) => Reader.GetStream(ordinal);
        public override int VisibleFieldCount => Reader.VisibleFieldCount;
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => Reader.IsDBNullAsync(ordinal, cancellationToken);
#if NET5_0_OR_GREATER
        [Obsolete]
#endif
        public override object InitializeLifetimeService() => Reader.InitializeLifetimeService();

#if NET6_0_OR_GREATER
        public override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default) => Reader.GetColumnSchemaAsync(cancellationToken);
        public override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default) => Reader.GetSchemaTableAsync(cancellationToken);
#endif

#if NETCOREAPP3_1_OR_GREATER
        public override Task CloseAsync() => IsClosed ? Task.CompletedTask : CloseAsyncImpl();
        private async Task CloseAsyncImpl()
        {
            commandFactory.PostProcess(state.Command!, args);
            if (commandFactory.TryRecycle(state.Command!))
            {
                state.Command = null;
            }
            var reader = state.Reader;
            if (reader is not null) await reader.CloseAsync();
            await state.DisposeAsync();
            await base.CloseAsync();
        }
#endif
    }
}

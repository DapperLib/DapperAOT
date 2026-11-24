#if NET6_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test;

// Executes the DbBatch code paths of Command<TArgs> against a fake provider, so the
// transaction/timeout/sub-batching behavior is verified without a real database.
public class BatchExecutionTests
{
    private static Command<string> GetCommand(FakeConnection connection, DbTransaction? transaction = null, int timeout = 0)
        => new(connection, transaction, "insert into [Foo] (Name) values (@name)", 0, timeout, RecordingHandler.Instance);

    [Fact]
    public void BatchUsesTransactionAndTimeout()
    {
        using var connection = new FakeConnection();
        using var transaction = connection.BeginTransaction();
        var command = GetCommand(connection, transaction, timeout: 42);

        Assert.Equal(3, command.Execute(new[] { "a", "b", "c" }, batchSize: 10));

        var batch = Assert.Single(connection.Batches);
        Assert.Same(transaction, batch.Transaction);
        Assert.Equal(42, batch.Timeout);
        Assert.Equal(3, batch.Commands.Count);
        Assert.Equal(1, batch.ExecutionCount);
    }

    [Fact]
    public async Task BatchUsesTransactionAndTimeoutAsync()
    {
        using var connection = new FakeConnection();
        using var transaction = connection.BeginTransaction();
        var command = GetCommand(connection, transaction, timeout: 42);

        Assert.Equal(3, await command.ExecuteAsync(new[] { "a", "b", "c" }, batchSize: 10));

        var batch = Assert.Single(connection.Batches);
        Assert.Same(transaction, batch.Transaction);
        Assert.Equal(42, batch.Timeout);
        Assert.Equal(3, batch.Commands.Count);
        Assert.Equal(1, batch.ExecutionCount);
    }

    [Fact]
    public void NegativeBatchSizeUsesSingleBatch()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);

        Assert.Equal(5, command.Execute(new[] { "a", "b", "c", "d", "e" }, batchSize: -1));

        var batch = Assert.Single(connection.Batches);
        Assert.Equal(5, batch.Commands.Count);
        Assert.All(connection.Batches, b => Assert.Equal(1, b.ExecutionCount));
    }

    [Fact]
    public void ZeroBatchSizeDoesNotUseBatches()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);

        Assert.Equal(3, command.Execute(new[] { "a", "b", "c" }, batchSize: 0));

        Assert.Empty(connection.Batches);
        Assert.Equal(3, connection.CommandsExecuted);
    }

    [Fact]
    public void PositiveBatchSizeSplitsIntoSubBatches()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);

        Assert.Equal(5, command.Execute(new[] { "a", "b", "c", "d", "e" }, batchSize: 2));

        Assert.Equal(3, connection.Batches.Count);
        Assert.Equal(new[] { 2, 2, 1 }, connection.Batches.Select(b => b.Commands.Count));
        Assert.All(connection.Batches, b => Assert.Equal(1, b.ExecutionCount));
        // every recorded post-process saw its own command's row count
        Assert.Equal(new[] { "a", "b", "c", "d", "e" }, RecordingHandler.PostProcessed.Select(x => x.Arg));
        Assert.All(RecordingHandler.PostProcessed, x => Assert.Equal(1, x.RowCount));
    }

    [Fact]
    public void PositiveBatchSizeSplitsIntoSubBatches_Enumerable()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);
        IEnumerable<string> source() { yield return "a"; yield return "b"; yield return "c"; yield return "d"; yield return "e"; }

        Assert.Equal(5, command.Execute(source(), batchSize: 2));

        Assert.Equal(new[] { 2, 2, 1 }, connection.Batches.Select(b => b.Commands.Count));
    }

    [Fact]
    public async Task PositiveBatchSizeSplitsIntoSubBatches_Async()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);

        Assert.Equal(5, await command.ExecuteAsync(new[] { "a", "b", "c", "d", "e" }, batchSize: 2));

        Assert.Equal(3, connection.Batches.Count);
        Assert.Equal(new[] { 2, 2, 1 }, connection.Batches.Select(b => b.Commands.Count));
        Assert.All(connection.Batches, b => Assert.Equal(1, b.ExecutionCount));
        Assert.Equal(new[] { "a", "b", "c", "d", "e" }, RecordingHandler.PostProcessed.Select(x => x.Arg));
    }

    [Fact]
    public async Task EmptyBatchDoesNotOpenConnection()
    {
        using var connection = new FakeConnection();
        var command = GetCommand(connection);

        Assert.Equal(0, await command.ExecuteAsync(Array.Empty<string>(), batchSize: 2));
        Assert.Equal(0, command.Execute(Enumerable.Empty<string>(), batchSize: 2));

        Assert.Empty(connection.Batches);
        Assert.Equal(0, connection.OpenCount);
    }

    public BatchExecutionTests() => RecordingHandler.PostProcessed.Clear();

    internal sealed class RecordingHandler : CommandFactory<string>
    {
        public static readonly RecordingHandler Instance = new();
        private RecordingHandler() { }

        public static List<(string Arg, int RowCount)> PostProcessed { get; } = new();

        public override bool RequirePostProcess => true;

        public override void PostProcess(in UnifiedCommand command, string args, int rowCount)
            => PostProcessed.Add((args, rowCount));

        public override void AddParameters(in UnifiedCommand command, string name)
        {
            var p = command.CreateParameter();
            p.ParameterName = "name";
            p.DbType = DbType.String;
            p.Size = 400;
            p.Value = name;
            command.Parameters.Add(p);
        }

        public override void UpdateParameters(in UnifiedCommand command, string name)
        {
            command.Parameters[0].Value = name;
        }
    }

    internal sealed class FakeConnection : DbConnection
    {
        public List<FakeBatch> Batches { get; } = new();
        public int OpenCount { get; private set; }
        public int CommandsExecuted { get; private set; }
        private ConnectionState state = ConnectionState.Closed;

        public override string ConnectionString { get; set; } = "";
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => state;

        public override bool CanCreateBatch => true;
        protected override DbBatch CreateDbBatch()
        {
            var batch = new FakeBatch { Connection = this };
            Batches.Add(batch);
            return batch;
        }

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() => state = ConnectionState.Closed;
        public override void Open() { OpenCount++; state = ConnectionState.Open; }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new FakeTransaction(this, isolationLevel);

        protected override DbCommand CreateDbCommand() => new FakeCommand(this, cmd => CommandsExecuted++);
    }

    internal sealed class FakeTransaction : DbTransaction
    {
        private readonly FakeConnection connection;
        public FakeTransaction(FakeConnection connection, IsolationLevel isolationLevel)
        {
            this.connection = connection;
            IsolationLevel = isolationLevel;
        }
        public override IsolationLevel IsolationLevel { get; }
        protected override DbConnection DbConnection => connection;
        public override void Commit() { }
        public override void Rollback() { }
    }

    internal sealed class FakeCommand : DbCommand
    {
        private readonly DbConnection connection;
        private readonly Action<FakeCommand> onExecute;

        public FakeCommand(DbConnection connection, Action<FakeCommand> onExecute)
        {
            this.connection = connection;
            this.onExecute = onExecute;
        }

        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new FakeParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery() { onExecute(this); return 1; }
        public override object? ExecuteScalar() { onExecute(this); return 1; }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    internal sealed class FakeBatch : DbBatch
    {
        private readonly FakeBatchCommandCollection commands = new();

        public override int Timeout { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbBatchCommandCollection DbBatchCommands => commands;
        public IReadOnlyList<FakeBatchCommand> Commands => commands.Items;
        public int ExecutionCount { get; private set; }

        public override void Prepare() { }
        public override Task PrepareAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public override object? ExecuteScalar() { ExecutionCount++; return 0; }
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) => Task.FromResult<object?>(ExecuteScalar());

        protected override DbBatchCommand CreateDbBatchCommand() => new FakeBatchCommand(); // the base CreateBatchCommand adds it to BatchCommands

        public override int ExecuteNonQuery()
        {
            ExecutionCount++;
            foreach (var command in commands.Items)
            {
                command.SetRecordsAffected(1);
            }
            return commands.Items.Count;
        }

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ExecuteNonQuery());

        public override void Cancel() { }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class FakeBatchCommandCollection : DbBatchCommandCollection
    {
        public List<FakeBatchCommand> Items { get; } = new();
        public override int Count => Items.Count;
        public override bool IsReadOnly => false;

        protected override DbBatchCommand GetBatchCommand(int index) => Items[index];
        protected override void SetBatchCommand(int index, DbBatchCommand batchCommand) => Items[index] = (FakeBatchCommand)batchCommand;

        public override void Add(DbBatchCommand item) => Items.Add((FakeBatchCommand)item);
        public override void Clear() => Items.Clear();
        public override bool Contains(DbBatchCommand item) => Items.Contains(item);
        public override void CopyTo(DbBatchCommand[] array, int index) { foreach (var item in Items) array[index++] = item; }
        public override IEnumerator<DbBatchCommand> GetEnumerator() => Items.GetEnumerator();
        public override int IndexOf(DbBatchCommand item) => Items.IndexOf((FakeBatchCommand)item);
        public override void Insert(int index, DbBatchCommand item) => Items.Insert(index, (FakeBatchCommand)item);
        public override bool Remove(DbBatchCommand item) => Items.Remove((FakeBatchCommand)item);
        public override void RemoveAt(int index) => Items.RemoveAt(index);
    }

    internal sealed class FakeBatchCommand : DbBatchCommand
    {
        private int recordsAffected;
        private readonly FakeParameterCollection parameters = new();

        public override string CommandText { get; set; } = "";
        public override int RecordsAffected => recordsAffected;
        public void SetRecordsAffected(int value) => recordsAffected = value;
        public override CommandType CommandType { get; set; }
        protected override DbParameterCollection DbParameterCollection => parameters;
        public override bool CanCreateParameter => true;
        public override DbParameter CreateParameter() => new FakeParameter();
    }

    internal sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; } = "";
        public override string SourceColumn { get; set; } = "";
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    internal sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> items = new();

        public override int Count => items.Count;
        public override object SyncRoot => ((ICollection)items).SyncRoot;

        public override int Add(object value) { items.Add((DbParameter)value); return items.Count - 1; }
        public override void AddRange(Array values) { foreach (var value in values) items.Add((DbParameter)value!); }
        public override void Clear() => items.Clear();
        public override bool Contains(object value) => items.Contains((DbParameter)value);
        public override bool Contains(string value) => items.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => items.GetEnumerator();
        protected override DbParameter GetParameter(int index) => items[index];
        protected override DbParameter GetParameter(string parameterName) => items.First(p => p.ParameterName == parameterName);
        public override int IndexOf(object value) => items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => items.RemoveAt(IndexOf(parameterName));
        protected override void SetParameter(int index, DbParameter value) => items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => items[IndexOf(parameterName)] = value;
    }
}
#endif

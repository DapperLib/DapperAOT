using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class CommandRewriteBenchmarks : IAsyncDisposable
{
    private readonly NpgsqlConnection npgsql = new();

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();


    public CommandRewriteBenchmarks()
    {
        _postgresContainer.StartAsync().GetAwaiter().GetResult(); // yes, I know
        npgsql.ConnectionString = _postgresContainer.GetConnectionString();
        npgsql.Open();
        npgsql.Execute("""
            CREATE TABLE IF NOT EXISTS RewriteCustomers(
                 Id     integer GENERATED ALWAYS AS IDENTITY,
                 Name   varchar(40) NOT NULL
            );
            """);
    }

    [GlobalSetup]
    public void Setup()
    {
        npgsql.Execute("TRUNCATE RewriteCustomers RESTART IDENTITY;");
    }

    public class MyArgsType
    {
        public string? Name0 { get; set; }
        public string? Name1 { get; set; }
        public string? Name2 { get; set; }
        public string? Name3 { get; set; }
    }

    public static readonly MyArgsType Args = new() { Name0 = "abc", Name1 = "def", Name2 = "ghi", Name3 = "jkl" };

    // for our test, we're going to do 4 operations and try rewriting it as batch
    public const string BasicSql = """
        insert into RewriteCustomers (Name) values (@Name0);
        insert into RewriteCustomers (Name) values (@Name1);
        insert into RewriteCustomers (Name) values (@Name2);
        insert into RewriteCustomers (Name) values (@Name3);
        """;

    [Benchmark(Baseline = true)]
    public int Dapper() => npgsql.Execute(BasicSql, Args);

    // note these are hand written

    [Benchmark]
    public int DapperAOT() => npgsql.Command<MyArgsType>(BasicSql, handler: BasicCommand.NonCached).Execute(Args);

    [Benchmark]
    public int DapperAOT_Cached() => npgsql.Command<MyArgsType>(BasicSql, handler: BasicCommand.Cached).Execute(Args);

    [Benchmark]
    public int DapperAOT_Batch() => npgsql.Command<MyArgsType>(BasicSql, handler: RewriteCommand.NonCached).Execute(Args);

    [Benchmark]
    public int DapperAOT_BatchCached() => npgsql.Command<MyArgsType>(BasicSql, handler: RewriteCommand.Cached).Execute(Args);

    static DbCommand CreateCommand(DbConnection connection)
    {
        // note that connection.CreateCommand has a stash for command recycling,
        // that isn't available if you "new" etc
        var cmd = connection.CreateCommand();
        cmd.Connection = connection;
        cmd.CommandText = BasicSql;
        cmd.CommandType = CommandType.Text;

        var ps = cmd.Parameters;
        var p = cmd.CreateParameter();
        p.ParameterName = "Name0";
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name0;
        ps.Add(p);

        p = cmd.CreateParameter();
        p.ParameterName = "Name1";
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name1;
        ps.Add(p);

        p = cmd.CreateParameter();
        p.ParameterName = "Name2";
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name2;
        ps.Add(p);

        p = cmd.CreateParameter();
        p.ParameterName = "Name3";
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name3;
        ps.Add(p);

        return cmd;
    }

    static void UpdateCommand(DbCommand cmd, DbConnection connection)
    {
        cmd.Connection = connection;
        var ps = cmd.Parameters;
        ps[0].Value = Args.Name0;
        ps[1].Value = Args.Name1;
        ps[2].Value = Args.Name2;
        ps[3].Value = Args.Name3;
    }

    [Benchmark]
    public int AdoNetCommand()
    {
        using var cmd = CreateCommand(npgsql);
        return cmd.ExecuteNonQuery();
    }

    static DbCommand? _spareCommand;

    [Benchmark]
    public int AdoNetCommandCached()
    {
        var cmd = Interlocked.Exchange(ref _spareCommand, null);
        if (cmd is null)
        {
            cmd = CreateCommand(npgsql);
        }
        else
        {
            UpdateCommand(cmd, npgsql);
        }
        var result = cmd.ExecuteNonQuery();
        cmd.Connection = null;
        Interlocked.Exchange(ref _spareCommand, cmd)?.Dispose();
        return result;
    }
    private static NpgsqlBatch CreateBatch(NpgsqlConnection connection)
    {
        // note that CreateBatch has obj reuse that new() lacks
        var batch = connection.CanCreateBatch ? connection.CreateBatch() : new NpgsqlBatch();
        batch.Connection = connection;
        var cmds = batch.BatchCommands;

        var cmd = batch.CreateBatchCommand();
        cmd.CommandText = "insert into RewriteCustomers(Name) values($1)";
        cmd.CommandType = CommandType.Text;
        var p = cmd.CanCreateParameter ? cmd.CreateParameter() : new NpgsqlParameter();
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name0;
        cmd.Parameters.Add(p);
        cmds.Add(cmd);

        cmd = batch.CreateBatchCommand();
        cmd.CommandText = "insert into RewriteCustomers(Name) values($1)";
        cmd.CommandType = CommandType.Text;
        p = cmd.CanCreateParameter ? cmd.CreateParameter() : new NpgsqlParameter();
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name1;
        cmd.Parameters.Add(p);
        cmds.Add(cmd);

        cmd = batch.CreateBatchCommand();
        cmd.CommandText = "insert into RewriteCustomers(Name) values($1)";
        cmd.CommandType = CommandType.Text;
        p = cmd.CanCreateParameter ? cmd.CreateParameter() : new NpgsqlParameter();
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name2;
        cmd.Parameters.Add(p);
        cmds.Add(cmd);

        cmd = batch.CreateBatchCommand();
        cmd.CommandText = "insert into RewriteCustomers(Name) values($1)";
        cmd.CommandType = CommandType.Text;
        p = cmd.CanCreateParameter ? cmd.CreateParameter() : new NpgsqlParameter();
        p.DbType = DbType.String;
        p.Size = -1;
        p.Value = Args.Name3;
        cmd.Parameters.Add(p);
        cmds.Add(cmd);

        return batch;
    }

    static void UpdateBatch(NpgsqlBatch batch, NpgsqlConnection connection)
    {
        batch.Connection = connection;
        var commands = batch.BatchCommands;
        commands[0].Parameters[0].Value = Args.Name0;
        commands[1].Parameters[0].Value = Args.Name1;
        commands[2].Parameters[0].Value = Args.Name2;
        commands[3].Parameters[0].Value = Args.Name3;
    }

    static NpgsqlBatch? _spareBatch;

    [Benchmark]
    public int AdoNetBatch()
    {
        using var batch = CreateBatch(npgsql);
        return batch.ExecuteNonQuery();
    }

    [Benchmark]
    public int AdoNetBatchCached()
    {
        var batch = Interlocked.Exchange(ref _spareBatch, null);
        if (batch is null)
        {
            batch = CreateBatch(npgsql);
        }
        else
        {
            UpdateBatch(batch, npgsql);
        }
        var result = batch.ExecuteNonQuery();
        batch.Connection = null;
        Interlocked.Exchange(ref _spareBatch, batch)?.Dispose();
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await npgsql.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class BasicCommand : CommandFactory<MyArgsType>
    {
        private readonly bool cached;
        private BasicCommand(bool cached) => this.cached = cached;
        public static BasicCommand NonCached { get; } = new BasicCommand(false);
        public static BasicCommand Cached { get; } = new BasicCommand(true);

        public override void AddParameters(in UnifiedCommand command, MyArgsType args)
        {
            var p = command.AddParameter();
            p.ParameterName = "Name0";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name0);

            p = command.AddParameter();
            p.ParameterName = "Name1";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name1);

            p = command.AddParameter();
            p.ParameterName = "Name2";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name2);

            p = command.AddParameter();
            p.ParameterName = "Name3";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name3);
        }

        public override void UpdateParameters(in UnifiedCommand command, MyArgsType args)
        {
            var ps = command.Parameters;
            ps[0].Value = AsValue(args.Name0);
            ps[1].Value = AsValue(args.Name1);
            ps[2].Value = AsValue(args.Name2);
            ps[3].Value = AsValue(args.Name3);
        }

        private static DbCommand? _spareCommand;
        public override bool TryRecycle(DbCommand command)
            => cached && TryRecycle(ref _spareCommand, command);


        public override DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, MyArgsType args)
            => (cached ? TryReuse(ref _spareCommand, sql, commandType, args) : null)
                ?? base.GetCommand(connection, sql, commandType, args);
    }

    private sealed class RewriteCommand : CommandFactory<MyArgsType>
    {
        private readonly bool cached;
        private RewriteCommand(bool cached) => this.cached = cached;
        public static RewriteCommand Cached { get; } = new RewriteCommand(true);
        public static RewriteCommand NonCached { get; } = new RewriteCommand(false);

        public override void AddParameters(in UnifiedCommand command, MyArgsType args)
            => throw new NotSupportedException(); // we don't expect to get here (in reality, we would have both versions)

        public override bool UseBatch(string sql) => sql == BasicSql; // assert that we're doing the right thing

        public override void AddCommands(in UnifiedBatch batch, string sql, MyArgsType args)
        {
            // the first command is initialized automatically
            batch.SetCommand("insert into RewriteCustomers(Name) values($1)");
            var p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name0);

            // the fact that the NpgSql command is the same is a coincidence of the test
            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name1);

            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name2);

            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name3);
        }

        public override void UpdateParameters(in UnifiedBatch command, MyArgsType args)
        {
            command[0][0].Value = AsValue(args.Name0);
            command[1][0].Value = AsValue(args.Name1);
            command[2][0].Value = AsValue(args.Name2);
            command[3][0].Value = AsValue(args.Name3);
        }

        private static DbBatch? _spareBatch;

        public override bool TryRecycle(DbBatch batch)
            => cached && TryRecycle(ref _spareBatch, batch);

        public override DbBatch GetBatch(DbConnection connection, string sql, CommandType commandType, MyArgsType args)
             => (cached ? TryReuse(ref _spareBatch, args) : null)
                ?? base.GetBatch(connection, sql, commandType, args);
    }
}
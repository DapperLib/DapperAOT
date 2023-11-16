using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using UsageBenchmark;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class BatchInsertBenchmarks : IAsyncDisposable
{
    private readonly SqlConnection sqlClient = new(Program.ConnectionString);
    private readonly NpgsqlConnection npgsql = new();

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private Customer[] customers = [];

    public void DebugState()
    {
        // waiting on binaries from https://github.com/dotnet/SqlClient/pull/1825
        Console.WriteLine($"{nameof(sqlClient.CanCreateBatch)}: {sqlClient.CanCreateBatch}");
        if (sqlClient.CanCreateBatch)
        {
            using var batch = sqlClient.CreateBatch();
            var cmd = batch.CreateBatchCommand();
            Console.WriteLine($"{nameof(cmd.CanCreateParameter)}: {cmd.CanCreateParameter}");
        }
    }

    public BatchInsertBenchmarks()
    {
        try { sqlClient.Execute("drop table BenchmarkCustomers;"); } catch { }
        sqlClient.Execute("create table BenchmarkCustomers(Id int not null identity(1,1), Name nvarchar(200) not null);");

        _postgresContainer.StartAsync().GetAwaiter().GetResult(); // yes, I know
        npgsql.ConnectionString = _postgresContainer.GetConnectionString();

        npgsql.Execute("""
            CREATE TABLE IF NOT EXISTS BenchmarkCustomers(
                 Id     integer GENERATED ALWAYS AS IDENTITY,
                 Name   varchar(40) NOT NULL
            );
            """);
    }

    [Params(0, 1, 10, 100, 1000)]
    public int Count { get; set; }

    [Params(false, true)]
    public bool IsOpen { get; set; } = true;

    [GlobalSetup]
    public void Setup()
    {
        var arr = new Customer[Count];
        for (int i = 1; i <= arr.Length; i++)
        {
            arr[i - 1] = new Customer { Id = i, Name = "Name " + i };
        }
        customers = arr;
        if (IsOpen)
        {
            sqlClient.Open();
            npgsql.Open();
        }
        else
        {
            sqlClient.Close();
            npgsql.Close();
        }
        sqlClient.Execute("truncate table BenchmarkCustomers;");
        npgsql.Execute("TRUNCATE BenchmarkCustomers RESTART IDENTITY;");
    }

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int Dapper() => sqlClient.Execute("insert BenchmarkCustomers (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Sync", "SqlClient"), DapperAot, CacheCommand]
    public int DapperAot() => sqlClient.Execute("insert BenchmarkCustomers (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int DapperAotManual() => sqlClient.Command("insert BenchmarkCustomers (Name) values (@name)",
        handler: CustomHandler.Unprepared).Execute(customers);

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int DapperAot_PreparedManual() => sqlClient.Command("insert BenchmarkCustomers (Name) values (@name)",
        handler: CustomHandler.Prepared).Execute(customers);

    [Benchmark(Baseline = true), BenchmarkCategory("Sync", "SqlClient")]
    public int Manual()
    {
        if (customers.Length == 0) return 0;
        bool close = false;
        try
        {
            if (sqlClient.State != ConnectionState.Open)
            {
                sqlClient.Open();
                close = true;
            }
            var cmd = GetManualCommand(sqlClient, out var name);
            if (customers.Length != 1) cmd.Prepare();

            int total = 0;
            foreach (var customer in customers)
            {
                name.Value = customer.Name ?? (object)DBNull.Value;
                total += cmd.ExecuteNonQuery();
            }
            if (!RecycleManual(cmd)) cmd.Dispose();
            return total;
        }
        finally
        {
            if (close) sqlClient.Close();
        }
    }

    [Benchmark, BenchmarkCategory("Sync", "Npgsql"), DapperAot, CacheCommand, BatchSize(0)]
    public int NpgsqlDapperAotNoBatch() => npgsql.Execute("insert into BenchmarkCustomers (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Sync", "Npgsql"), DapperAot, CacheCommand, BatchSize(-1)]
    public int NpgsqlDapperAotFullBatch() => npgsql.Execute("insert into BenchmarkCustomers (Name) values (@name)", customers);

    private static DbCommand? _spare;
    private static DbCommand GetManualCommand(DbConnection connection, out DbParameter name)
    {
        var cmd = Interlocked.Exchange(ref _spare, null);
        if (cmd is null)
        {
            cmd = connection.CreateCommand();
            cmd.CommandText = "insert BenchmarkCustomers (Name) values (@name)";
            cmd.CommandType = CommandType.Text;
            name = cmd.CreateParameter();
            name.ParameterName = "name";
            name.DbType = DbType.String;
            name.Size = 400;
            name.Value = "";
            cmd.Parameters.Add(name);
        }
        else
        {
            name = cmd.Parameters[0];
        }
        cmd.Connection = connection;
        return cmd;
    }

    private static bool RecycleManual(DbCommand cmd)
        => Interlocked.CompareExchange(ref _spare, cmd, null) is null;

    [Benchmark, BenchmarkCategory("Async", "SqlClient")]
    public Task<int> DapperAsync() => sqlClient.ExecuteAsync("insert BenchmarkCustomers (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Async", "SqlClient")]
    public Task<int> DapperAotAsync() => sqlClient.Command("insert BenchmarkCustomers (Name) values (@name)",
        handler: CustomHandler.Unprepared).ExecuteAsync(customers);

    [Benchmark, BenchmarkCategory("Async", "SqlClient")]
    public Task<int> DapperAot_PreparedAsync() => sqlClient.Command("insert BenchmarkCustomers (Name) values (@name)",
        handler: CustomHandler.Prepared).ExecuteAsync(customers);

    [Benchmark(Baseline = true), BenchmarkCategory("Async", "SqlClient")]
    public async Task<int> ManualAsync()
    {
        if (customers.Length == 0) return 0;
        bool close = false;
        try
        {
            if (sqlClient.State != ConnectionState.Open)
            {
                await sqlClient.OpenAsync();
                close = true;
            }
            var cmd = GetManualCommand(sqlClient, out var name);
            if (customers.Length != 1)
            {
                await cmd.PrepareAsync();
            }

            int total = 0;
            foreach (var customer in customers)
            {
                name.Value = customer.Name ?? (object)DBNull.Value;
                total += await cmd.ExecuteNonQueryAsync();
            }

            if (!RecycleManual(cmd))
            {
                await cmd.DisposeAsync();
            }
            return total;
        }
        finally
        {
            if (close)
            {
                await sqlClient.CloseAsync();
            }
        }
    }

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int EntityFramework()
    {
        using var ctx = new MyContext();
        ctx.Customers.AddRange(customers);
        var result = ctx.SaveChanges();
        ctx.ChangeTracker.Clear();
        return result;
    }

    [Benchmark, BenchmarkCategory("Async", "SqlClient")]
    public async Task<int> EntityFrameworkAsync()
    {
        using var ctx = new MyContext();
        ctx.Customers.AddRange(customers);
        var result = await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();
        return result;
    }

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int SqlBulkCopyFastMember()
    {
        bool close = false;
        try
        {
            if (sqlClient.State != ConnectionState.Open)
            {
                sqlClient.Open();
                close = true;
            }
            using var table = new SqlBulkCopy(sqlClient)
            {
                DestinationTableName = "BenchmarkCustomers",
                ColumnMappings =
            {
                { nameof(Customer.Name), nameof(Customer.Name) }
            }
            };
            table.EnableStreaming = true;
            table.WriteToServer(FastMember.ObjectReader.Create(customers, nameof(Customer.Name)));
            return table.RowsCopied;
        }
        finally
        {
            if (close) sqlClient.Close();
        }
    }

    [Benchmark, BenchmarkCategory("Sync", "SqlClient")]
    public int SqlBulkCopyDapper()
    {
        bool close = false;
        try
        {
            if (sqlClient.State != ConnectionState.Open)
            {
                sqlClient.Open();
                close = true;
            }
            using var table = new SqlBulkCopy(sqlClient)
            {
                DestinationTableName = "BenchmarkCustomers",
                ColumnMappings =
                {
                    { nameof(Customer.Name), nameof(Customer.Name) }
                }
            };
            table.EnableStreaming = true;
            table.WriteToServer(TypeAccessor.CreateDataReader(customers, [nameof(Customer.Name)]));
            return table.RowsCopied;
        }
        finally
        {
            if (close) sqlClient.Close();
        }
    }

    [Benchmark, BenchmarkCategory("Async", "SqlClient")]
    public async Task<int> SqlBulkCopyFastMemberAsync()
    {
        bool close = false;
        try
        {
            if (sqlClient.State != ConnectionState.Open)
            {
                await sqlClient.OpenAsync();
                close = true;
            }
            using var table = new SqlBulkCopy(sqlClient)
            {
                DestinationTableName = "BenchmarkCustomers",
                ColumnMappings =
            {
                { nameof(Customer.Name), nameof(Customer.Name) }
            }
            };
            table.EnableStreaming = true;
            await table.WriteToServerAsync(FastMember.ObjectReader.Create(customers, nameof(Customer.Name)));
            return table.RowsCopied;
        }
        finally
        {
            if (close) await sqlClient.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await sqlClient.DisposeAsync();
        await npgsql.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class CustomHandler : CommandFactory<Customer>
    {
        public static readonly CustomHandler Prepared = new(true), Unprepared = new(false);
        private readonly bool _canPrepare;
        private CustomHandler(bool canPrepare) => _canPrepare = canPrepare;

        private static DbCommand? _spareP, _spareU;

        private ref DbCommand? Spare => ref (_canPrepare ? ref _spareP : ref _spareU);

        public override DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, Customer args)
            => TryReuse(ref Spare, sql, commandType, args) ?? base.GetCommand(connection, sql, commandType, args);

        public override bool TryRecycle(DbCommand command) => TryRecycle(ref Spare, command);

        public override void AddParameters(in UnifiedCommand command, Customer obj)
        {
            var p = command.CreateParameter();
            p.ParameterName = "name";
            p.DbType = DbType.String;
            p.Size = 400;
            p.Value = AsValue(obj.Name);
            command.Parameters.Add(p);
        }

        public override void UpdateParameters(in UnifiedCommand command, Customer obj)
        {
            command.Parameters[0].Value = AsValue(obj.Name);
        }

        public override bool CanPrepare => _canPrepare;
    }
}
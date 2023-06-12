using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using UsageBenchmark;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class BatchInsertBenchmarks : IDisposable
{
    public SqlConnection connection = new(Program.ConnectionString);
    private Customer[] customers = Array.Empty<Customer>();

    public BatchInsertBenchmarks()
    {
        try { connection.Execute("drop table BenchmarkBatchInsert;"); } catch { }
        connection.Execute("create table BenchmarkBatchInsert(Id int not null identity(1,1), Name nvarchar(200) not null);");
    }

    [Params(0, 1, 10, 100, 1000)]
    public int Count { get; set; }

    [Params(false, true)]
    public bool IsOpen { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var arr = new Customer[Count];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new Customer { Id = i, Name = "Name " + i };
        }
        customers = arr;
        if (IsOpen)
        {
            connection.Open();
        }
        else
        {
            connection.Close();
        }
        connection.Execute("truncate table BenchmarkBatchInsert;");
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Sync")]
    public void Dapper() => connection.Execute("insert BenchmarkBatchInsert (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Sync")]
    public void DapperAot() => connection.Batch("insert BenchmarkBatchInsert (Name) values (@name)",
        handler: CustomHandler.Unprepared).Execute(customers);

    [Benchmark, BenchmarkCategory("Sync")]
    public void DapperAot_Prepared() => connection.Batch("insert BenchmarkBatchInsert (Name) values (@name)",
        handler: CustomHandler.Prepared).Execute(customers);

    [Benchmark, BenchmarkCategory("Sync")]
    public void Manual()
    {
        if (customers.Length != 0)
        {
            bool close = false;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                    close = true;
                }
                var cmd = GetManualCommand(connection, out var name);
                if (customers.Length != 1) cmd.Prepare();

                foreach (var customer in customers)
                {
                    name.Value = customer.Name ?? (object)DBNull.Value;
                    cmd.ExecuteNonQuery();
                }
                if (!RecycleManual(cmd)) cmd.Dispose();
            }
            finally
            {
                if (close) connection.Close();
            }
        }
    }

    private static DbCommand? _spare;
    private static DbCommand GetManualCommand(DbConnection connection, out DbParameter name)
    {
        var cmd = Interlocked.Exchange(ref _spare, null);
        if (cmd is null)
        {
            cmd = connection.CreateCommand();
            cmd.CommandText = "insert BenchmarkBatchInsert (Name) values (@name)";
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

    [Benchmark(Baseline = true), BenchmarkCategory("Async")]
    public Task DapperAsync() => connection.ExecuteAsync("insert BenchmarkBatchInsert (Name) values (@name)", customers);

    [Benchmark, BenchmarkCategory("Async")]
    public Task DapperAotAsync() => connection.Batch("insert BenchmarkBatchInsert (Name) values (@name)",
        handler: CustomHandler.Unprepared).ExecuteAsync(customers);

    [Benchmark, BenchmarkCategory("Async")]
    public Task DapperAot_PreparedAsync() => connection.Batch("insert BenchmarkBatchInsert (Name) values (@name)",
        handler: CustomHandler.Prepared).ExecuteAsync(customers);

    [Benchmark, BenchmarkCategory("Async")]
    public async Task ManualAsync()
    {
        if (customers.Length != 0)
        {
            bool close = false;
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                    close = true;
                }
                var cmd = GetManualCommand(connection, out var name);
                if (customers.Length != 1)
                {
                    await cmd.PrepareAsync();
                }

                foreach (var customer in customers)
                {
                    name.Value = customer.Name ?? (object)DBNull.Value;
                    await cmd.ExecuteNonQueryAsync();
                }

                if (!RecycleManual(cmd))
                {
                    await cmd.DisposeAsync();
                }
            }
            finally
            {
                if (close)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }

    public void Dispose() => connection.Dispose();
}


public class Customer
{
    public int Id { get; set; }

    [DbValue(Size = 400)]
    public string Name { get; set; }
}

internal class CustomHandler : CommandFactory<Customer>
{
    public static readonly CustomHandler Prepared = new(true), Unprepared = new(false);
    private readonly bool _canPrepare;
    private CustomHandler(bool canPrepare) => _canPrepare = canPrepare;

    private static DbCommand? _spareP, _spareU;

    private ref DbCommand? Spare => ref (_canPrepare ? ref _spareP : ref _spareU);

    public override DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, Customer args)
    {
        var cmd = Interlocked.Exchange(ref Spare, null);
        if (cmd is not null)
        {
            UpdateParameters(cmd, args);
            return cmd;
        }
        return base.GetCommand(connection, sql, commandType, args);
    }

    public override bool TryRecycle(DbCommand command)
        => Interlocked.CompareExchange(ref Spare, command, null) is null;

    public override void AddParameters(DbCommand command, Customer obj)
    {
        var p = command.CreateParameter();
        p.ParameterName = "name";
        p.DbType = DbType.String;
        p.Size = 400;
        p.Value = AsValue(obj.Name);
        command.Parameters.Add(p);
    }

    public override void UpdateParameters(DbCommand command, Customer obj)
    {
        command.Parameters[0].Value = AsValue(obj.Name);
    }

    public override bool CanPrepare => _canPrepare;
}
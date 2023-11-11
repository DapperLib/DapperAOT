using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using UsageBenchmark;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class QueryBenchmarks : IDisposable
{
    private readonly SqlConnection connection = new(Program.ConnectionString);

    public QueryBenchmarks()
    {
        try { connection.Execute("drop table BenchmarkCustomers;"); } catch { }
        connection.Execute("create table BenchmarkCustomers(Id int not null identity(1,1), Name nvarchar(200) not null);");
    }

    [Params(0, 1, 10, 100, 1000)]
    public int Count { get; set; }

    [Params(false, true)]
    public bool IsOpen { get; set; }

    [GlobalSetup, DapperAot]
    public void Setup()
    {
        var arr = new Customer[Count];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new Customer { Id = i, Name = "Name " + i };
        }
        if (IsOpen)
        {
            connection.Open();
        }
        else
        {
            connection.Close();
        }
        connection.Execute("truncate table BenchmarkCustomers;");

        connection.Execute("insert BenchmarkCustomers (Name) values (@name)", arr);
    }

    // note these are materialization tests; I know we could get the count more directly; that's *not the point*
#pragma warning disable DAP219 // SELECT columns should be specified explicitly
    [Benchmark, BenchmarkCategory("Sync")]
    public int DapperDynamic() => connection.Query("select * from BenchmarkCustomers").AsList().Count;

    [Benchmark, BenchmarkCategory("Sync")]
    public int Dapper() => connection.Query<Customer>("select * from BenchmarkCustomers").AsList().Count;

    [Benchmark, BenchmarkCategory("Sync"), DapperAot, CacheCommand]
    public int DapperAotDynamic() => connection.Query("select * from BenchmarkCustomers").AsList().Count;

    [Benchmark, BenchmarkCategory("Sync"), DapperAot, CacheCommand]
    public int DapperAot() => connection.Query<Customer>("select * from BenchmarkCustomers").AsList().Count;

    [Benchmark, BenchmarkCategory("Async")]
    public async Task<int> DapperDynamicAsync() => (await connection.QueryAsync("select * from BenchmarkCustomers")).AsList().Count;

    [Benchmark, BenchmarkCategory("Async")]
    public async Task<int> DapperAsync() => (await connection.QueryAsync<Customer>("select * from BenchmarkCustomers")).AsList().Count;

    [Benchmark, BenchmarkCategory("Async"), DapperAot, CacheCommand]
    public async Task<int> DapperAotDynamicAsync() => (await connection.QueryAsync("select * from BenchmarkCustomers")).AsList().Count;

    [Benchmark, BenchmarkCategory("Async"), DapperAot, CacheCommand]
    public async Task<int> DapperAotAsync() => (await connection.QueryAsync<Customer>("select * from BenchmarkCustomers;")).AsList().Count;
#pragma warning restore DAP219

    [Benchmark, BenchmarkCategory("Sync")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needed for test")]
    public int EntityFramework()
    {
        using var ctx = new MyContext();
        return ctx.Customers.ToList().Count;
    }

    [Benchmark, BenchmarkCategory("Async")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needed for test")]
    public async Task<int> EntityFrameworkAsync()
    {
        using var ctx = new MyContext();
        return (await ctx.Customers.ToListAsync()).Count;
    }

    public void Dispose()
    {
        connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
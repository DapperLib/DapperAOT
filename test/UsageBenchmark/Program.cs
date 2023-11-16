using Dapper;
using System;
using System.Threading.Tasks;

namespace UsageBenchmark;

static class Program
{
    public const string ConnectionString = "Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True";

#if RELEASE
    static void Main(string[] args) => BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#else
    static async Task Main()
    {
        await using (var obj = new BatchInsertBenchmarks())
        {
            await RunAllInserts(obj, 10, false);
            await RunAllInserts(obj, 10, true);
        }

        using (var obj = new QueryBenchmarks())
        {
            await RunAllQueries(obj, 10, false);
            await RunAllQueries(obj, 10, true);
        }

        static async Task RunAllInserts(BatchInsertBenchmarks obj, int count, bool isOpen)
        {
            obj.Count = count;
            obj.IsOpen = isOpen;
            obj.Setup();

            obj.DebugState();

            Console.WriteLine(obj.Manual());
            Console.WriteLine(await obj.ManualAsync());

            Console.WriteLine(obj.Dapper());
            Console.WriteLine(await obj.DapperAsync());

            Console.WriteLine(obj.DapperAot());
            Console.WriteLine(await obj.DapperAotAsync());

            Console.WriteLine(obj.DapperAotManual());
            Console.WriteLine(await obj.DapperAotAsync());

            Console.WriteLine(obj.DapperAot_PreparedManual());
            Console.WriteLine(await obj.DapperAot_PreparedAsync());

            Console.WriteLine(obj.EntityFramework());
            Console.WriteLine(await obj.EntityFrameworkAsync());

            Console.WriteLine(obj.SqlBulkCopyFastMember());
            Console.WriteLine(obj.SqlBulkCopyDapper());
            Console.WriteLine(await obj.SqlBulkCopyFastMemberAsync());

            Console.WriteLine(obj.NpgsqlDapperAotNoBatch());
            Console.WriteLine(obj.NpgsqlDapperAotFullBatch());
        }

        static async Task RunAllQueries(QueryBenchmarks obj, int count, bool isOpen)
        {
            obj.Count = count;
            obj.IsOpen = isOpen;
            obj.Setup();

            Console.WriteLine(obj.DapperDynamic());
            Console.WriteLine(obj.Dapper());
            Console.WriteLine(obj.DapperAotDynamic());
            Console.WriteLine(obj.DapperAot());
            Console.WriteLine(obj.EntityFramework());

            Console.WriteLine(await obj.DapperDynamicAsync());
            Console.WriteLine(await obj.DapperAsync());
            Console.WriteLine(await obj.DapperAotDynamicAsync());
            Console.WriteLine(await obj.DapperAotAsync());
            Console.WriteLine(await obj.EntityFrameworkAsync());
        }
    }
#endif
}
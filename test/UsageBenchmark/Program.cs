using BenchmarkDotNet.Running;
using Dapper;
using System;
using System.Threading.Tasks;

namespace UsageBenchmark;

static class Program
{
    public const string ConnectionString = "Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True";

#if RELEASE
    static void Main() => BenchmarkRunner.Run(typeof(Program).Assembly);
#else
    static async Task Main()
    {
        using var obj = new BatchInsertBenchmarks();
        await RunAll(obj, 10, false);
        await RunAll(obj, 10, true);

        static async Task RunAll(BatchInsertBenchmarks obj, int count, bool isOpen)
        {
            obj.Count = count;
            obj.IsOpen = isOpen;
            obj.Setup();

            obj.ResetIds();
            Console.WriteLine(obj.Manual());
            obj.ResetIds();
            Console.WriteLine(await obj.ManualAsync());

            obj.ResetIds();
            Console.WriteLine(obj.Dapper());
            obj.ResetIds();
            Console.WriteLine(await obj.DapperAsync());

            obj.ResetIds();
            Console.WriteLine(obj.DapperAot());
            obj.ResetIds();
            Console.WriteLine(await obj.DapperAotAsync());

            obj.ResetIds();
            Console.WriteLine(obj.DapperAot_Prepared());
            obj.ResetIds();
            Console.WriteLine(await obj.DapperAot_PreparedAsync());

            obj.ResetIds();
            Console.WriteLine(obj.EntityFramework());
            obj.ResetIds();
            Console.WriteLine(await obj.EntityFrameworkAsync());

            obj.ResetIds();
            Console.WriteLine(obj.SqlBulkCopyFastMember());
            obj.ResetIds();
            Console.WriteLine(await obj.SqlBulkCopyFastMemberAsync());
        }
    }
#endif
}
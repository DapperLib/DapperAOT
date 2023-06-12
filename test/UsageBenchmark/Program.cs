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

            Console.WriteLine(obj.Manual());
            Console.WriteLine(await obj.ManualAsync());

            Console.WriteLine(obj.Dapper());
            Console.WriteLine(await obj.DapperAsync());

            Console.WriteLine(obj.DapperAot());
            Console.WriteLine(await obj.DapperAotAsync());

            Console.WriteLine(obj.DapperAot_Prepared());
            Console.WriteLine(await obj.DapperAot_PreparedAsync());

            Console.WriteLine(obj.EntityFramework());
            Console.WriteLine(await obj.EntityFrameworkAsync());
        }
    }
#endif
}
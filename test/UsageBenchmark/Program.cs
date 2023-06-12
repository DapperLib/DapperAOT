using BenchmarkDotNet.Running;
using Dapper;
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

            obj.Manual();
            await obj.ManualAsync();

            obj.Dapper();
            await obj.DapperAsync();

            obj.DapperAot();
            await obj.DapperAotAsync();

            obj.DapperAot_Prepared();
            await obj.DapperAot_PreparedAsync();
        }
    }
#endif
}
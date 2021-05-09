using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Dapper;
using System;
using System.Data.Common;

[module: LegacyMaterializer(true)]
namespace UsageBenchmark
{
    class Program
    {
        static void Main()
            => BenchmarkRunner.Run(typeof(Program).Assembly);
    }

    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    public class DataAccess : IDisposable
    {
        private DbConnection? _msData, _systemData;

        [GlobalSetup]
        public void Connect()
        {
            const string cs = @"Data Source=.\SQLEXPRESS;Initial Catalog=master;Integrated Security=True";
            _msData = new Microsoft.Data.SqlClient.SqlConnection(cs);
            _msData.Open();
            _systemData = new System.Data.SqlClient.SqlConnection(cs);
            _systemData.Open();

            try
            {
                _msData.Execute("drop table DapperCustomers;");
            }
            catch { }
            _msData.Execute(@"create table DapperCustomers
(
	Id int not null primary key,
	Region nvarchar(50) not null,
	[Name] nvarchar(max) not null
);");
            _msData.Execute(@"insert DapperCustomers(Id, Region, Name) values (@Id, @Region, @Name);",
                new Customer { Id = Id, Region = Region, Name = "Test" });
        }

        const int Id = 42;
        const string Region = "North";
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
        {
            _msData?.Dispose();
            _systemData?.Dispose();
        }

        [Benchmark]
        public Customer DapperSystemData()
            => _systemData.QueryFirstOrDefault<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region });

        [Benchmark]
        public Customer DapperMicrosoftData()
            => _msData.QueryFirstOrDefault<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region });

        [Benchmark]
        public Customer DapperAOTSystemData()
            => DapperAOT.GetCustomer(_systemData!, Id, Region);

        [Benchmark]
        public Customer DapperAOTMicrosoftData()
            => DapperAOT.GetCustomer(_msData!, Id, Region);
    }

    public static partial class DapperAOT
    {
        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial Customer GetCustomer(DbConnection connection, int id, string region);
    }
    public sealed class Customer
    {
        // [Column("CustomerId")]
        public int Id { get; init; }
        public string? Name { get; init; }
        public string? Region { get; init; }
    }

}

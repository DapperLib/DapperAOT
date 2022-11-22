using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Running;
using Dapper;
using Dapper.Samples.DapperDbBenchmark;
using PooledAwait;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

// [module: LegacyMaterializer(true)]
namespace UsageBenchmark
{
    class Program
    {
        static void Main()
            => BenchmarkRunner.Run(typeof(Program).Assembly);
        //{
        //    var obj = new DapperDbDriver();
        //    obj.Connect();
        //    await Console.Out.WriteLineAsync("MSD...");
        //    foreach (var item in await obj.MSDLoadFortunesRows())
        //    {
        //        await Console.Out.WriteLineAsync($"{item.Id}: {item.Message}");
        //    }
        //    foreach (var item in await obj.MSDLoadMultipleQueriesRows())
        //    {
        //        await Console.Out.WriteLineAsync($"{item.Id}: {item.RandomNumber}");
        //    }
        //    var w = await obj.MSDLoadSingleQueryRow();
        //    await Console.Out.WriteLineAsync($"{w.Id}: {w.RandomNumber}");
        //    foreach (var item in await obj.MSDLoadMultipleUpdatesRows())
        //    {
        //        await Console.Out.WriteLineAsync($"{item.Id}: {item.RandomNumber}");
        //    }

        //    //await Console.Out.WriteLineAsync("SD...");
        //    //await obj.SDLoadFortunesRows();
        //    //await obj.SDLoadMultipleQueriesRows();
        //    //await obj.SDLoadSingleQueryRow();
        //    //await obj.SDLoadMultipleUpdatesRows();
        //    //await Console.Out.WriteLineAsync("done...");
        //}
    }

    [MemoryDiagnoser]
    [CategoriesColumn]
    public class DapperDbDriver
    {
        private IDb? _msData, _systemData;

        [GlobalSetup]
        public void Connect()
        {
            var random = new Random(12345);
            const string cs = @"Data Source=.\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True";
            _systemData = new DapperDb(random, System.Data.SqlClient.SqlClientFactory.Instance, cs);
            _msData = new DapperDb(random, Microsoft.Data.SqlClient.SqlClientFactory.Instance, cs);

            //var sb = new StringBuilder().Append("truncate table world; truncate table fortune;").AppendLine();
            //using var conn = new System.Data.SqlClient.SqlConnection(cs);
            //for (int i = 0; i < 50; i++)
            //{
            //    sb.Append($"insert fortune (id, message) values ({i}, 'message {i}');");
            //}
            //for (int i = 0; i <= 10001; i++)
            //{
            //    sb.Append($"insert world (id, randomnumber) values ({i}, {random.Next(1, 10000)});");
            //}
            //conn.Open();
            //using var cmd = conn.CreateCommand();
            //cmd.Connection = conn;
            //cmd.CommandText = sb.ToString();
            //cmd.ExecuteNonQuery();
        }

        [Benchmark, BenchmarkCategory("MSD")]
        public Task MSDLoadFortunesRows() => _msData!.LoadFortunesRows();
        [Benchmark, BenchmarkCategory("SD")]
        public Task SDLoadFortunesRows() => _systemData!.LoadFortunesRows();

        [Benchmark, BenchmarkCategory("MSD")]
        public Task MSDLoadMultipleQueriesRows() => _msData!.LoadMultipleQueriesRows(10);
        [Benchmark, BenchmarkCategory("SD")]
        public Task SDLoadMultipleQueriesRows() => _systemData!.LoadMultipleQueriesRows(10);

        [Benchmark, BenchmarkCategory("MSD")]
        public Task MSDLoadSingleQueryRow() => _msData!.LoadSingleQueryRow();
        [Benchmark, BenchmarkCategory("SD")]
        public Task SDLoadSingleQueryRow() => _systemData!.LoadSingleQueryRow();

        [Benchmark, BenchmarkCategory("MSD")]
        public Task MSDLoadMultipleUpdatesRows() => _msData!.LoadMultipleUpdatesRows(10);
        [Benchmark, BenchmarkCategory("SD")]
        public Task SDLoadMultipleUpdatesRows() => _systemData!.LoadMultipleUpdatesRows(10);
    }
    //[SimpleJob(RuntimeMoniker.NetCoreApp50)]
    [MemoryDiagnoser]
    [CategoriesColumn]
    public class DataAccess : IDisposable
    {
        private DbConnection? _msData, _systemData;

        [GlobalSetup]
        public void Connect()
        {
            const string cs = @"Data Source=.\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True";
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

        [BenchmarkCategory("Sync", "SysData", "Dapper")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DapperSystemData()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = _systemData.QueryFirstOrDefault<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region });
        }

        [BenchmarkCategory("Sync", "MSData", "Dapper")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DapperMicrosoftData()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = _msData.QueryFirstOrDefault<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region });
        }

        [BenchmarkCategory("Sync", "SysData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DapperAOTSystemData()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = DapperAOT.GetCustomer(_systemData!, Id, Region);
        }

        [BenchmarkCategory("Sync", "MSData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public void DapperAOTMicrosoftData()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = DapperAOT.GetCustomer(_msData!, Id, Region);
        }


        private const int OperationsPerInvoke = 1000;
        [BenchmarkCategory("Task", "SysData", "Dapper")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperSystemData_Async()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await _systemData.QueryFirstOrDefaultAsync<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region }).ConfigureAwait(false);
        }

        [BenchmarkCategory("Task", "MSData", "Dapper")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperMicrosoftData_Async()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await _msData.QueryFirstOrDefaultAsync<Customer>(@"select * from DapperCustomers where Id=@id and Region=@region", new { Id, Region }).ConfigureAwait(false);
        }

        [BenchmarkCategory("Task", "SysData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTSystemData_Task()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerTaskAsync(_systemData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("Task", "MSData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTMicrosoftData_Task()
        {
            for (int i = 0; i < OperationsPerInvoke;i++)
                _ = await DapperAOT.GetCustomerTaskAsync(_msData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("ValueTask", "SysData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTSystemData_ValueTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerValueTaskAsync(_systemData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("ValueTask", "MSData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTMicrosoftData_ValueTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerValueTaskAsync(_msData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("PooledValueTask", "SysData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTSystemData_PooledValueTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerPooledValueTaskAsync(_systemData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("PooledValueTask", "MSData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTMicrosoftData_PooledValueTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerPooledValueTaskAsync(_msData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("PooledTask", "SysData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTSystemData_PooledTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerPooledTaskAsync(_systemData!, Id, Region).ConfigureAwait(false);
        }

        [BenchmarkCategory("PooledTask", "MSData", "DapperAOT")]
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public async Task DapperAOTMicrosoftData_PooledTask()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
                _ = await DapperAOT.GetCustomerPooledTaskAsync(_msData!, Id, Region).ConfigureAwait(false);
        }
    }

    public static partial class DapperAOT
    {
        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial Customer GetCustomer(DbConnection connection, int id, string region);

        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial Task<Customer> GetCustomerTaskAsync(DbConnection connection, int id, string region);

        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial ValueTask<Customer> GetCustomerValueTaskAsync(DbConnection connection, int id, string region);

        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial PooledTask<Customer> GetCustomerPooledTaskAsync(DbConnection connection, int id, string region);

        [Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
        [SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
        public static partial PooledValueTask<Customer> GetCustomerPooledValueTaskAsync(DbConnection connection, int id, string region);
    }
    public sealed class Customer
    {
        // [Column("CustomerId")]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Region { get; set; }

        ////TODO generate this automatically
        //internal sealed class CustomerReader : TypeReader<Customer>
        //{
        //    [ModuleInitializer]
        //    internal static void Register() => Register(Instance);
        //    private CustomerReader() { }
        //    internal static CustomerReader Instance { get; } = new CustomerReader();
        //    protected override int GetColumnToken(string name, Type? type, bool isNullable)
        //    {
        //        switch (name)
        //        {
        //            case nameof(Id):
        //                if (type == typeof(int)) return isNullable ? 0 : 1;
        //                return 2;
        //            case nameof(Name):
        //                if (type == typeof(int)) return isNullable ? 3 : 4;
        //                return 5;
        //            case nameof(Region):
        //                if (type == typeof(int)) return isNullable ? 6 : 7;
        //                return 8;
        //            default:
        //                return NoField;
        //        }
        //    }

        //    public override Customer Read(DbDataReader reader, ReadOnlySpan<int> tokens, int offset)
        //    {
        //        int Id = default;
        //        string? Name = default;
        //        string? Region = default;
        //        foreach (var token in tokens)
        //        {
        //            switch (token)
        //            {
        //                case 0: Id = reader.GetInt32(offset); break;
        //                case 1: Id = reader.GetInt32(offset); break;
        //                case 2: Id = Convert.ToInt32(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //                case 3: Name = reader.GetString(offset); break;
        //                case 4: Name = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
        //                case 5: Name = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //                case 6: Region = reader.GetString(offset); break;
        //                case 7: Region = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
        //                case 8: Region = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //            }
        //            offset++;
        //        }
        //        return new Customer
        //        {
        //            Id = Id,
        //            Name = Name,
        //            Region = Region
        //        };
        //    }

        //    protected override Customer ReadFallback(IDataReader reader, ReadOnlySpan<int> tokens, int offset)
        //    {
        //        int Id = default;
        //        string? Name = default;
        //        string? Region = default;
        //        foreach (var token in tokens)
        //        {
        //            switch (token)
        //            {
        //                case 0: Id = reader.GetInt32(offset); break;
        //                case 1: Id = reader.GetInt32(offset); break;
        //                case 2: Id = Convert.ToInt32(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //                case 3: Name = reader.GetString(offset); break;
        //                case 4: Name = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
        //                case 5: Name = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //                case 6: Region = reader.GetString(offset); break;
        //                case 7: Region = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
        //                case 8: Region = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
        //            }
        //            offset++;
        //        }
        //        return new Customer
        //        {
        //            Id = Id,
        //            Name = Name,
        //            Region = Region
        //        };
        //    }
        //}
    }
}

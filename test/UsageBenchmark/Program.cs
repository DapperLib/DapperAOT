using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// [module: LegacyMaterializer(true)]
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
            TypeReader.Register(new Customer.CustomerReader());
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

        //TODO generate this automatically
        public sealed class CustomerReader : TypeReader<Customer>
        {
            protected override int GetColumnToken(string name, Type? type, bool isNullable)
            {
                switch (name)
                {
                    case nameof(Id):
                        if (type == typeof(int)) return isNullable ? 0 : 1;
                        return 2;
                    case nameof(Name):
                        if (type == typeof(int)) return isNullable ? 3 : 4;
                        return 5;
                    case nameof(Region):
                        if (type == typeof(int)) return isNullable ? 6 : 7;
                        return 8;
                    default:
                        return NoField;
                }
            }

            public override Customer Read(DbDataReader reader, ReadOnlySpan<int> tokens, int offset)
            {
                int Id = default;
                string? Name = default;
                string? Region = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0: Id = reader.GetInt32(offset); break;
                        case 1: Id = reader.GetInt32(offset); break;
                        case 2: Id = Convert.ToInt32(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 3: Name = reader.GetString(offset); break;
                        case 4: Name = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
                        case 5: Name = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 6: Region = reader.GetString(offset); break;
                        case 7: Region = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
                        case 8: Region = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                    }
                    offset++;
                }
                return new Customer
                {
                    Id = Id,
                    Name = Name,
                    Region = Region
                };
            }

            protected override Customer ReadFallback(IDataReader reader, ReadOnlySpan<int> tokens, int offset)
            {
                int Id = default;
                string? Name = default;
                string? Region = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0: Id = reader.GetInt32(offset); break;
                        case 1: Id = reader.GetInt32(offset); break;
                        case 2: Id = Convert.ToInt32(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 3: Name = reader.GetString(offset); break;
                        case 4: Name = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
                        case 5: Name = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 6: Region = reader.GetString(offset); break;
                        case 7: Region = reader.IsDBNull(offset) ? null : reader.GetString(offset); break;
                        case 8: Region = reader.IsDBNull(offset) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                    }
                    offset++;
                }
                return new Customer
                {
                    Id = Id,
                    Name = Name,
                    Region = Region
                };
            }

            public override async ValueTask<Customer> ReadAsync(DbDataReader reader, ArraySegment<int> tokens, CancellationToken cancellationToken)
            {
                int Id = default;
                string? Name = default;
                string? Region = default;

                var end = tokens.Offset + tokens.Count;
                var arr = tokens.Array!;
                for (int offset = tokens.Offset; offset < end; offset++)
                {
                    switch (arr[offset])
                    {
                        case 0: Id = await reader.GetFieldValueAsync<int>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 1: Id = await reader.GetFieldValueAsync<int>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 2: Id = Convert.ToInt32(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 3: Name = await reader.GetFieldValueAsync<string>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 4: Name = await reader.IsDBNullAsync(offset, cancellationToken).ConfigureAwait(false) ? null : await reader.GetFieldValueAsync<string>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 5: Name = await reader.IsDBNullAsync(offset, cancellationToken).ConfigureAwait(false) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                        case 6: Region = await reader.GetFieldValueAsync<string>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 7: Region = await reader.IsDBNullAsync(offset, cancellationToken).ConfigureAwait(false) ? null : await reader.GetFieldValueAsync<string>(offset, cancellationToken).ConfigureAwait(false); break;
                        case 8: Region = await reader.IsDBNullAsync(offset, cancellationToken).ConfigureAwait(false) ? null : Convert.ToString(reader.GetValue(offset), CultureInfo.InvariantCulture); break;
                    }
                }
                return new Customer
                {
                    Id = Id,
                    Name = Name,
                    Region = Region
                };
            }
        }
    }

}

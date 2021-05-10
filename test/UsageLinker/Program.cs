using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;

[module: LegacyMaterializer(false)]
namespace UsageLinker
{
    static class Program
	{
		static void Main()
		{
            //TypeReader.Register(Customer.CustomerReader.Instance);

            using var conn = new SqlConnection("gibberish");
			var c = Customer.GetCustomer(conn, 42, "South");
			Console.WriteLine(c.Name);
		}
	}
	public sealed partial class Customer
	{
		[Command(@"select * from DapperCustomers where Id=@id and Region=@region")]
		[SingleRow(SingleRowKind.FirstOrDefault)] // entirely optional; to influence what happens when zero/multiple rows returned
		public static partial Customer GetCustomer(DbConnection connection, int id, string region);


		// [Column("CustomerId")]
		public int Id { get; init; }
		public string? Name { get; init; }
		public string? Region { get; init; }

        public sealed class CustomerReader : TypeReader<Customer>
        {
            private CustomerReader() { }
            public static CustomerReader Instance { get; } = new CustomerReader();
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
        }
    }
}

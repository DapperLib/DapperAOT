using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;

namespace UsageLinker
{
	static class Program
	{
		static void Main()
		{
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
	}
}

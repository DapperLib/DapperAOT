using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

partial class Test
{
	public class FooParams // must be ref type
	{
		public int Id { get; set; }
		[Parameter(Direction = ParameterDirection.InputOutput)]
		public string? InputOutput { get; set; }
		[Parameter(Direction = ParameterDirection.Output)]
		public DateTime Output { get; set; }
	}

	[Command("sproc")]
	[return: Parameter(Direction = ParameterDirection.ReturnValue)]
	public partial ValueTask<int> ReturnViaReturnAsync(DbConnection connection, FooParams parameters);

	[Command("sproc")]
	[return: RecordsAffected]
	public partial ValueTask<int> RecordsAffectedViaReturnAsync(DbConnection connection, FooParams parameters);

	public class BarParams : FooParams // must be ref type
	{
		[Parameter(Direction = ParameterDirection.ReturnValue)]
		public int Count { get; set; }

		[RecordsAffected]
		public int RecordsAffected { get; set; }
	}

	[Command("sproc")]
	public partial ValueTask ReturnAndRecordsAffectedViaOutAsync(DbConnection connection, BarParams parameters);

	[Command("sproc")]
	public partial ValueTask FineControlAsync(DbConnection connection, [Parameter(Name = "x", Precision = 4, Scale = 2, Size = 3)] decimal value);
}
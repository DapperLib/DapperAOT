using Dapper;
using System;
using System.Data;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	[return: Parameter(Direction = ParameterDirection.ReturnValue)]
	public partial int ReturnViaReturn(DbConnection connection, int id, ref string inputOutput, out DateTime output);

	[Command("sproc")]
	[RecordsAffected]
	public partial int RecordsAffectedViaReturn(DbConnection connection, int id, ref string inputOutput, out DateTime output);

	[Command("sproc")]
	public partial void ReturnAndRecordsAffectedViaOut(DbConnection connection, int id, ref string inputOutput, out DateTime output,
		[Parameter(Direction = ParameterDirection.ReturnValue)] out int count
		[RecordsAffected] out int recordsAffected);

	[Command("sproc")]
	public partial void FineControl(DbConnection connection, [Parameter(Name = "x", Precision = 4, Scale = 2, Size = 3)] decimal value);
}
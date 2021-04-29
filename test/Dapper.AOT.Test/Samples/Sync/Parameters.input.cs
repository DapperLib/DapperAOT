using Dapper;
using System;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	[return: ReturnValue]
	public partial int ReturnViaReturn(DbConnection connection, int id, ref string inputOutput, out DateTime output);

	[Command("sproc")]
	public partial void ReturnViaOut(DbConnection connection, int id, ref string inputOutput, out DateTime output, [ReturnValue] out int count);

	[Command("sproc")]
	public partial void FineControl(DbConnection connection, [Parameter(Name = "x", Precision = 4, Scale = 2, Size = 3)] decimal value);
}
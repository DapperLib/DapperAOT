using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection.Metadata;

partial class Test
{
	[Command("sproc")]
	[return: Parameter(Direction = ParameterDirection.ReturnValue)]
	public partial int ReturnViaReturn(DbConnection connection, int id, ref string inputOutput, out DateTime output);

	[Command("sproc")]
	public partial void ReturnViaOut(DbConnection connection, int id, ref string inputOutput, out DateTime output, [Parameter(Direction = ParameterDirection.ReturnValue)] out int count);

	[Command("sproc")]
	public partial void FineControl(DbConnection connection, [Parameter(Name = "x", Precision = 4, Scale = 2, Size = 3)] decimal value);
}
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

partial class Test
{
	[Command]
	public partial List<SomeType> AdHocCommandText([Command] string sql, DbConnection connection, int id, string name);

	[Command(CommandType = CommandType.StoredProcedure)]
	public partial List<SomeType> AdHocStoredProcedure([Command] string sql, DbConnection connection, int id, string name);

	[Command("this is CommandText")]
	public partial List<SomeType> ImplicitCommandText(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial List<SomeType> ImplicitStoredProcedure(DbConnection connection, int id, string name);

	[Command("this is StoredProc", CommandType = CommandType.StoredProcedure)]
	public partial List<SomeType> ExplicitStoredProcedure(DbConnection connection, int id, string name);
}
public class SomeType { }
using Dapper;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
#nullable enable

partial class Test
{
	[Command("sproc")]
	public partial void Abstract(DbConnection connection);

	[Command("sproc")]
	public partial void Interface(IDbConnection connection);

	[Command("sproc")]
	public partial void Concrete(SqlConnection connection);

	[Command("sproc")]
	public partial void Transaction(DbTransaction transaction);

	[Command("sproc")] // connection used should be connection ?? transaction?.Connection
	public partial void OptionalTransaction(DbConnection connection, DbTransaction? transaction = null);
}
public class SomeType { }
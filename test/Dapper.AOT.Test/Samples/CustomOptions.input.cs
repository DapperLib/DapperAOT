using Dapper;
using SSqlConnection = System.Data.SqlClient.SqlConnection;
using MSqlConnection = Microsoft.Data.SqlClient.SqlConnection;

partial class Test
{
	[Command("sproc")]
	[Encryption(EncryptionKind.ResultSetOnly)]
	public partial SomeType WithEncryptionSystemSql(SSqlConnection connection, int id, string name);

	[Command("sproc")]
	[Encryption(EncryptionKind.Enabled)]
	public partial SomeType WithEncryptionMicrosoftSql(MSqlConnection connection, int id, string name);
}

public class SomeType { }
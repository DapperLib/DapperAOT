using Dapper;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	public partial void Void(DbConnection connection, int id, string name);
}
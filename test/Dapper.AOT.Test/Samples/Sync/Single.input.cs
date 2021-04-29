using Dapper;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	public partial SomeType Single(DbConnection connection, int id, string name);
}
public class SomeType { }
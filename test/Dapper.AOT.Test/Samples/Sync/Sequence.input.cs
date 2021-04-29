using Dapper;
using System.Collections.Generic;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	public partial IEnumerable<SomeType> Sequence(DbConnection connection, int id, string name);
}
public class SomeType { }
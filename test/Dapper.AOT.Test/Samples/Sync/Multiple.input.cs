using Dapper;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	public partial List<SomeType> List(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial SomeType[] Array(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial IList<SomeType> IList(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ICollection<SomeType> ICollection(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ImmutableArray<SomeType> ImmutableArray(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ImmutableList<SomeType> ImmutableList(DbConnection connection, int id, string name);
}
public class SomeType { }
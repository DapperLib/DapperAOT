using Dapper;
using System.Data.Common;

partial class Test
{
	[Command("sproc")]
	public partial SomeType ImplicitComplex(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial int ImplicitScalar(DbConnection connection, int id, string name);

	[Command("sproc"), SingleRow(SingleRowKind.First)]
	public partial SomeType First(DbConnection connection, int id, string name);

	[Command("sproc"), SingleRow(SingleRowKind.FirstOrDefault)]
	public partial SomeType FirstOrDefault(DbConnection connection, int id, string name);

	[Command("sproc"), SingleRow(SingleRowKind.Single)]
	public partial SomeType Single(DbConnection connection, int id, string name);

	[Command("sproc"), SingleRow(SingleRowKind.SingleOrDefault)]
	public partial SomeType SingleOrDefault(DbConnection connection, int id, string name);

	[Command("sproc"), SingleRow(SingleRowKind.Scalar)]
	public partial int Scalar(DbConnection connection, int id, string name);
}
public class SomeType { }
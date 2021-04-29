using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

partial class Test
{
	[Command("sproc")]
	public partial IAsyncEnumerable<SomeType> SequenceAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial IAsyncEnumerable<SomeType> SequenceWithCancellationAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);
}
public class SomeType { }
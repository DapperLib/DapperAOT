using Dapper;
using System.Data.Common;
using System.Threading;
using PooledAwait;

partial class Test
{
	[Command("sproc")]
	public partial PooledTask<SomeType> TaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial PooledValueTask<SomeType> ValueTaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial PooledTask<SomeType> TaskWithCancellationAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);

	[Command("sproc")]
	public partial PooledValueTask<SomeType> ValueWithCancellationTaskAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);
}
public class SomeType { }
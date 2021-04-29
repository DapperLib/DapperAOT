using Dapper;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

partial class Test
{
	[Command("sproc")]
	public partial Task<SomeType> TaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<SomeType> ValueTaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial Task<SomeType> TaskWithCancellationAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);

	[Command("sproc")]
	public partial ValueTask<SomeType> ValueWithCancellationTaskAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);
}
public class SomeType { }
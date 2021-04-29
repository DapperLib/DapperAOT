using Dapper;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

partial class Test
{
	[Command("sproc")]
	public partial Task TaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask ValueTaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial Task TaskWithCancellationAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);

	[Command("sproc")]
	public partial ValueTask ValueWithCancellationTaskAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);
}
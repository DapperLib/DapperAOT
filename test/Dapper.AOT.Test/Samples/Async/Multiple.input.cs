using Dapper;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

partial class Test
{
	[Command("sproc")]
	public partial Task<List<SomeType>> TaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<List<SomeType>> ValueTaskAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial Task<List<SomeType>> TaskWithCancellationAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);

	[Command("sproc")]
	public partial ValueTask<List<SomeType>> ValueWithCancellationTaskAsync(DbConnection connection, int id, string name, CancellationToken cancellation = default);

	[Command("sproc")]
	public partial ValueTask<SomeType[]> ArrayAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<IList<SomeType>> IListAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<ICollection<SomeType>> ICollectionAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<ImmutableArray<SomeType>> ImmutableArrayAsync(DbConnection connection, int id, string name);

	[Command("sproc")]
	public partial ValueTask<ImmutableList<SomeType>> ImmutableListAsync(DbConnection connection, int id, string name);
}
public class SomeType { }
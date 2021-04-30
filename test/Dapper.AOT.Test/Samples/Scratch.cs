using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

partial class P

{
	public partial IAsyncEnumerable<int> Foo(CancellationToken ct);

	public async partial IAsyncEnumerable<int> Foo([EnumeratorCancellation] CancellationToken ct)
	{
		yield return 1;
		await Task.Yield();
		yield return 2;
	}
}
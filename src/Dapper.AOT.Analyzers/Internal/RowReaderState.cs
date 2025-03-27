using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct RowReaderState : IEnumerable<(ITypeSymbol Type, ImmutableArray<string> QueryColumns, int Index)>
{
    public RowReaderState() { }
    private readonly Dictionary<(ITypeSymbol Type, ImmutableArray<string> QueryColumns), int> resultTypes = new ();

    public int Count() => resultTypes.Count();

    public IEnumerator<(ITypeSymbol Type, ImmutableArray<string> QueryColumns, int Index)> GetEnumerator()
    {
        // retain discovery order
        return resultTypes.OrderBy(x => x.Value).Select(x => (x.Key.Type, x.Key.QueryColumns, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(ITypeSymbol type, ImmutableArray<string> queryColumns)
    {
        if (!resultTypes.TryGetValue((type, queryColumns), out var index))
        {
            resultTypes.Add((type, queryColumns), index = resultTypes.Count);
        }
        return index;
    }
}

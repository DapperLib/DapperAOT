using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct RowReaderState : IEnumerable<(ITypeSymbol Type, int Index)>
{
    public RowReaderState() { }
    private readonly Dictionary<ITypeSymbol, int> resultTypes = new (SymbolEqualityComparer.Default);

    public int Count() => resultTypes.Count();

    public IEnumerator<(ITypeSymbol Type, int Index)> GetEnumerator()
    {
        // retain discovery order
        return resultTypes.OrderBy(x => x.Value).Select(x => (x.Key, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(ITypeSymbol type)
    {
        if (!resultTypes.TryGetValue(type, out var index))
        {
            resultTypes.Add(type, index = resultTypes.Count);
        }
        return index;
    }
}

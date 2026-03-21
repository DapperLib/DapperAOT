using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct RowReaderState : IEnumerable<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns, int Index)>
{
    public RowReaderState() { }
    private readonly Dictionary<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns), int> resultTypes = new (KeyComparer.Instance);

    public int Count() => resultTypes.Count;

    public IEnumerator<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns, int Index)> GetEnumerator()
    {
        // retain discovery order
        return resultTypes.OrderBy(x => x.Value).Select(x => (x.Key.Type, x.Key.Flags, x.Key.QueryColumns, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(ITypeSymbol type, OperationFlags flags, ImmutableArray<string> queryColumns)
    {
        const OperationFlags SIGNIFICANT_FLAGS = OperationFlags.StrictTypes; // restrict to flags that impact the reader
        var key = (type, flags & SIGNIFICANT_FLAGS, queryColumns);
        if (!resultTypes.TryGetValue(key, out var index))
        {
            resultTypes.Add(key, index = resultTypes.Count);
        }
        return index;
    }

    private sealed class KeyComparer : IEqualityComparer<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns)>
    {
        private KeyComparer() { }
        public static readonly KeyComparer Instance = new();

        bool IEqualityComparer<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns)>.Equals((ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns) x, (ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns) y)
            => SymbolEqualityComparer.Default.Equals(x.Type, y.Type) && x.Flags == y.Flags && AdditionalCommandState.Equals(x.QueryColumns, y.QueryColumns);

        int IEqualityComparer<(ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns)>.GetHashCode((ITypeSymbol Type, OperationFlags Flags, ImmutableArray<string> QueryColumns) obj)
            => SymbolEqualityComparer.Default.GetHashCode(obj.Type) ^ (int)obj.Flags ^ AdditionalCommandState.GetHashCode(obj.QueryColumns);
    }
}

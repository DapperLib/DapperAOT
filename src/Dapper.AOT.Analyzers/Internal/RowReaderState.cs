using Dapper.CodeAnalysis.Model;
using Dapper.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct RowReaderState : IEnumerable<(RowPlan Plan, OperationFlags Flags, int Index)>
{
    public RowReaderState() { }
    private readonly Dictionary<(RowPlan Plan, OperationFlags Flags), int> resultTypes = new(KeyComparer.Instance);

    public int Count() => resultTypes.Count;

    public IEnumerator<(RowPlan Plan, OperationFlags Flags, int Index)> GetEnumerator()
    {
        // retain discovery order
        return resultTypes.OrderBy(x => x.Value).Select(x => (x.Key.Plan, x.Key.Flags, x.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(RowPlan plan, OperationFlags flags)
    {
        const OperationFlags SIGNIFICANT_FLAGS = OperationFlags.StrictTypes; // restrict to flags that impact the reader
        var key = (plan, flags & SIGNIFICANT_FLAGS);
        if (!resultTypes.TryGetValue(key, out var index))
        {
            resultTypes.Add(key, index = resultTypes.Count);
        }
        return index;
    }

    private sealed class KeyComparer : IEqualityComparer<(RowPlan Plan, OperationFlags Flags)>
    {
        private KeyComparer() { }
        public static readonly KeyComparer Instance = new();

        bool IEqualityComparer<(RowPlan Plan, OperationFlags Flags)>.Equals((RowPlan Plan, OperationFlags Flags) x, (RowPlan Plan, OperationFlags Flags) y)
            => x.Plan.Equals(y.Plan) && x.Flags == y.Flags;

        int IEqualityComparer<(RowPlan Plan, OperationFlags Flags)>.GetHashCode((RowPlan Plan, OperationFlags Flags) obj)
            => obj.Plan.GetHashCode() ^ (int)obj.Flags;
    }
}

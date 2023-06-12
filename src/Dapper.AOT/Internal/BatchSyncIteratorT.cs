using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dapper.Internal;

internal ref struct BatchSyncIterator<T>
{
#if NET7_0_OR_GREATER
    private readonly int _count;
    private int _index;
    private readonly ref T _origin;
#else
    private readonly ReadOnlySpan<T> _span;
    private int _spanIndex;
#endif
    private readonly IEnumerator<T>? _iterator;

    public BatchSyncIterator(ReadOnlySpan<T> source)
    {
#if NET7_0_OR_GREATER
        _index = 0;
        _count = source.Length;
        _origin = ref MemoryMarshal.GetReference(source);
#else
        _span = source;
        _spanIndex = 0;
#endif
        _iterator = null;
    }

    public BatchSyncIterator(IEnumerable<T> source)
    {
#if NET7_0_OR_GREATER
        _index = _count = 0;
        _origin = Unsafe.NullRef<T>();
#else
        _span = default;
        _spanIndex = 0;
#endif
        _iterator = source.GetEnumerator();
    }
    public bool MoveNext(out T value)
    {
#if NET7_0_OR_GREATER
        if (_index < _count)
        {
            value = Unsafe.Add(ref _origin, _index++);
            return true;
        }
#else
        if (_spanIndex < _span.Length)
        {
            value = _span[_spanIndex++];
            return true;
        }
#endif
        if (_iterator is not null && _iterator.MoveNext())
        {
            value = _iterator.Current;
            return true;
        }
        value = default!;
        return false;
    }

    internal void Dispose() => _iterator?.Dispose();
}

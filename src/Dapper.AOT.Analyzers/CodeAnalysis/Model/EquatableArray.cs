using System;
using System.Collections;
using System.Collections.Generic;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// An immutable array with <b>structural</b> equality, for use in cached generator model values.
/// </summary>
/// <remarks>
/// Deliberately not <see cref="System.Collections.Immutable.ImmutableArray{T}"/>, whose equality
/// is reference-based and silently defeats incremental caching.
/// </remarks>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly T[]? _items;
    public static EquatableArray<T> Empty => default;

    public EquatableArray(T[] items) => _items = items is { Length: 0 } ? null : items;

    public int Length => _items?.Length ?? 0;
    public int Count => Length;
    public bool IsEmpty => Length == 0;
    public T this[int index] => _items![index];

    public bool Equals(EquatableArray<T> other)
    {
        var x = _items;
        var y = other._items;
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
        {
            if (!x[i].Equals(y[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_items is null) return 0;
        int hash = _items.Length;
        foreach (var item in _items)
        {
            hash = (hash * -47) + item.GetHashCode();
        }
        return hash;
    }

    public Enumerator GetEnumerator() => new(_items);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(_items ?? [])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (_items ?? []).GetEnumerator();

    public struct Enumerator
    {
        private readonly T[]? _items;
        private int _index;
        internal Enumerator(T[]? items)
        {
            _items = items;
            _index = -1;
        }
        public bool MoveNext() => _items is not null && ++_index < _items.Length;
        public readonly T Current => _items![_index];
    }
}

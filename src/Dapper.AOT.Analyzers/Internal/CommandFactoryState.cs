﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct CommandFactoryState : IEnumerable<(ITypeSymbol Type, string Map, int Index, int CacheCount)>
{
    public CommandFactoryState() { }
    private readonly Dictionary<(ITypeSymbol Type, string Map, bool Cached), (int Index, int CacheCount)> parameterTypes = new(ParameterTypeMapComparer.Instance);

    public int Count()
    {
        int total = 0;
        foreach (var pair in parameterTypes)
        {
            // 1 for the non-cached factory; 1 for each cached subclass
            total += pair.Value.CacheCount + 1;
        }
        return total;
    }

    public IEnumerator<(ITypeSymbol Type, string Map, int Index, int CacheCount)> GetEnumerator()
    {
        // retain discovery order
        return parameterTypes.OrderBy(x => x.Value.Index).Select(x => (x.Key.Type, x.Key.Map, x.Value.Index, x.Value.CacheCount)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(ITypeSymbol type, string map, bool cache, out int? subIndex)
    {
        var key = (type!, map, cache);
        int index;
        if (parameterTypes.TryGetValue(key, out var value))
        {
            index = value.Index;
            if (cache)
            {
                subIndex = value.CacheCount;
                parameterTypes[key] = new(index, value.CacheCount + 1);
            }
            else
            {
                subIndex = null;
            }
        }
        else
        {
            index = parameterTypes.Count;
            subIndex = cache ? 0 : null;
            parameterTypes.Add(key, (index, cache ? 1 : 0));
        }
        return index;
    }

    private sealed class ParameterTypeMapComparer : IEqualityComparer<(ITypeSymbol Type, string Map, bool Cached)>
    {
        public static readonly ParameterTypeMapComparer Instance = new();
        private ParameterTypeMapComparer() { }

        public bool Equals((ITypeSymbol Type, string Map, bool Cached) x, (ITypeSymbol Type, string Map, bool Cached) y)
            => StringComparer.InvariantCultureIgnoreCase.Equals(x.Map, y.Map)
            && SymbolEqualityComparer.Default.Equals(x.Type, y.Type)
            && x.Cached == y.Cached;

        public int GetHashCode((ITypeSymbol Type, string Map, bool Cached) obj)
            => (StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.Map)
            ^ SymbolEqualityComparer.Default.GetHashCode(obj.Type))
            * (obj.Cached ? -1 : 1);
            
    }
}

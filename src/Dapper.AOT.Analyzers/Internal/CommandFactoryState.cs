using Dapper.CodeAnalysis.Model;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct CommandFactoryState : IEnumerable<(ParamPlan Plan, string Map, int Index, int CacheCount, AdditionalCommandState? AdditionalCommandState)>
{

    public CommandFactoryState(Compilation compilation) => systemObject = ParamPlan.Create(compilation.GetSpecialType(SpecialType.System_Object))!;
    private readonly ParamPlan systemObject;
    private readonly Dictionary<(ParamPlan Plan, string Map, bool Cached, AdditionalCommandState? AdditionalCommandState), (int Index, int CacheCount)> parameterTypes = new(ParameterTypeMapComparer.Instance);

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

    public IEnumerator<(ParamPlan Plan, string Map, int Index, int CacheCount, AdditionalCommandState? AdditionalCommandState)> GetEnumerator()
    {
        // retain discovery order
        return parameterTypes.OrderBy(x => x.Value.Index).Select(x => (x.Key.Plan, x.Key.Map, x.Value.Index, x.Value.CacheCount, x.Key.AdditionalCommandState)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int GetIndex(ParamPlan plan, string map, bool cache, AdditionalCommandState? additionalCommandState, out int? subIndex)
    {
        if (string.IsNullOrWhiteSpace(map) && plan.IsReferenceType)
        {
            // just use object if there's nothing to map
            plan = systemObject;
        }
        var key = (plan!, map, cache, additionalCommandState);
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

    private sealed class ParameterTypeMapComparer : IEqualityComparer<(ParamPlan Plan, string Map, bool Cached, AdditionalCommandState? AdditionalCommandState)>
    {
        public static readonly ParameterTypeMapComparer Instance = new();
        private ParameterTypeMapComparer() { }

        public bool Equals((ParamPlan Plan, string Map, bool Cached, AdditionalCommandState? AdditionalCommandState) x, (ParamPlan Plan, string Map, bool Cached, AdditionalCommandState? AdditionalCommandState) y)
            => StringComparer.InvariantCultureIgnoreCase.Equals(x.Map, y.Map)
            && x.Plan.Equals(y.Plan)
            && Equals(x.AdditionalCommandState, y.AdditionalCommandState)
            && x.Cached == y.Cached;

        public int GetHashCode((ParamPlan Plan, string Map, bool Cached, AdditionalCommandState? AdditionalCommandState) obj)
            => (StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.Map)
            ^ obj.Plan.GetHashCode())
            ^ (obj.AdditionalCommandState is null ? 0 : obj.AdditionalCommandState.GetHashCode())
            * (obj.Cached ? -1 : 1);

    }
}

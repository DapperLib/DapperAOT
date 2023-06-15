using System;
using System.Collections.Generic;

namespace Dapper.Internal.Extensions;

internal static class EnumerableExtensions
{
    public static bool Contains<T>(this IEnumerable<T> items, Func<T, bool> condition)
    {
        if (items is null) return false;
        foreach (var item in items)
        {
            if (condition(item)) return true;
        }

        return false;
    }
}
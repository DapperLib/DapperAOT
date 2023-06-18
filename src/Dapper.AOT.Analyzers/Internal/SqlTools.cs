using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dapper.Internal;

internal static class SqlTools
{
    // [?@:]                 start with one of "? @ :" to denote parameter
    // (                     capturing group
    //      [\p{L}_]         underscore or letter character
    //      [\p{L}\p{N}_]*   any number of underscore, letter or number characters
    // )
    private static readonly Regex ParameterRegex = new(@"[?@:]([\p{L}_][\p{L}\p{N}_]*)", RegexOptions.Compiled | RegexOptions.Multiline);

    public static ImmutableHashSet<string> GetUniqueParameters(string sql)
        => ImmutableHashSet.Create(StringComparer.InvariantCultureIgnoreCase, GetParameters(sql));

    public static string[] GetParameters(string sql)
    {
        if (!ParameterRegex.IsMatch(sql))
        {
            return Array.Empty<string>();
        }
        var matches = ParameterRegex.Matches(sql);
        if (matches.Count == 0)
        {
            return Array.Empty<string>();
        }
        var arr = new string[matches.Count];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = matches[i].Groups[1].Value;
        }
        return arr;
    }
}

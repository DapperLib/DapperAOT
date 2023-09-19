using System;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Dapper.SqlAnalysis;

namespace Dapper.Internal;

internal static class SqlTools
{
    // [?@:]                 start with one of "? @ :" to denote parameter
    // (                     capturing group
    //      [\p{L}_]         underscore or letter character
    //      [\p{L}\p{N}_]*   any number of underscore, letter or number characters
    // )
    private const RegexOptions SharedRegexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant;
    private static readonly Regex ParameterRegex = new(@"[?@:$]([\p{L}_][\p{L}\p{N}_]*)", SharedRegexOptions);

    internal static readonly Regex LiteralTokens = new(@"(?<![\p{L}\p{N}_])\{=([\p{L}\p{N}_]+)\}", SharedRegexOptions);

    public static ImmutableHashSet<string> GetUniqueParameters(string? sql, out SqlParseOutputFlags flags)
        => ImmutableHashSet.Create(StringComparer.InvariantCultureIgnoreCase, GetParameters(sql, out flags));

    public static bool IncludeParameter(string map, string name, out bool test)
    {
        test = false;
        if (string.IsNullOrWhiteSpace(map))
        {
            return false;
        }
        if (map == "?")
        {
            test = true;
            return true;
        }
        if (map == "*")
        {
            return true;
        }
        int start = 0, index;
        while ((index = map.IndexOf(name, start, StringComparison.InvariantCultureIgnoreCase)) >= 0)
        {
            if (
                (index == 0 || map[index-1] == ' ') // isn't "foo" in "somefoo"
                &&
                ((index + name.Length == map.Length) || map[index + name.Length] == ' ') // isn't "foo" in "foothing"
                )
            {
                return true;
            }
            start = index + name.Length;
        }
        return false;

    }

    public static string[] GetParameters(string? sql, out SqlParseOutputFlags flags)
    {
        flags = SqlParseOutputFlags.None;
        if (string.IsNullOrWhiteSpace(sql))
        {
            return Array.Empty<string>();
        }

        if (sql!.IndexOf("return", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
            flags |= SqlParseOutputFlags.Return;
        }

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

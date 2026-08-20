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
    private static readonly Regex ParameterRegex = new(@"(?<![?@:$\p{L}\p{N}_])[?@:$]([\p{L}_][\p{L}\p{N}_]*)", SharedRegexOptions);
    public const string ParameterPrefixCharacters = "?@:$";

    internal static readonly Regex LiteralTokens = new(@"(?<![\p{L}\p{N}_])\{=([\p{L}\p{N}_]+)\}", SharedRegexOptions);

    public static ImmutableHashSet<string> GetUniqueParameters(string? sql, bool includeLiteralTokens = false)
        => ImmutableHashSet.Create(StringComparer.InvariantCultureIgnoreCase, GetParameters(sql, includeLiteralTokens));

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

    public static string[] GetParameters(string? sql, bool includeLiteralTokens = false)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return [];
        }

        var parameterMatches = ParameterRegex.Matches(sql);
        if (!includeLiteralTokens)
        {
            if (parameterMatches.Count == 0)
            {
                return [];
            }
            var parameters = new string[parameterMatches.Count];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = parameterMatches[i].Groups[1].Value;
            }
            return parameters;
        }

        var literalMatches = LiteralTokens.Matches(sql);
        if (parameterMatches.Count == 0 && literalMatches.Count == 0)
        {
            return [];
        }
        var arr = new string[parameterMatches.Count + literalMatches.Count];
        for (int i = 0; i < parameterMatches.Count; i++)
        {
            arr[i] = parameterMatches[i].Groups[1].Value;
        }
        for (int i = 0; i < literalMatches.Count; i++)
        {
            arr[parameterMatches.Count + i] = literalMatches[i].Groups[1].Value;
        }
        return arr;
    }
}

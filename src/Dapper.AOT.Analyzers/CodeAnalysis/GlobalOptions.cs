using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace Dapper.CodeAnalysis;

internal static class GlobalOptions
{
    public static class Keys
    {
        public const string GlobalOptions_DapperSqlSyntax = "dapper.sqlsyntax";
        public const string GlobalOptions_DapperDebugSqlParseInputFlags = "dapper.debug_mode_sql_parse_input_flags"; // this is for test purposes; if you find and use this: don't blame me!
        public const string ProjectProperties_DapperSqlSyntax = "build_property.Dapper_SqlSyntax";

        /// <summary>
        /// Set by the SDK when <c>PublishAot</c> is on, and compiler-visible by default - so we
        /// can tell a native-AOT project from a JIT one without shipping any build props of our
        /// own. <c>PublishAot</c> itself is *not* compiler-visible, which is why this stands in.
        /// </summary>
        public const string ProjectProperties_EnableAotAnalyzer = "build_property.EnableAotAnalyzer";
    }

    /// <inheritdoc cref="TargetsNativeAot(AnalyzerConfigOptionsProvider?)"/>
    public static bool TargetsNativeAot(this AnalyzerOptions? options)
        => options?.AnalyzerConfigOptionsProvider.TargetsNativeAot() ?? false;

    /// <summary>
    /// Is the consuming project headed for native AOT? Decides whether a call-site we leave on
    /// vanilla Dapper is a missed optimization or a latent publish-time crash.
    /// </summary>
    public static bool TargetsNativeAot(this AnalyzerConfigOptionsProvider? provider)
        => provider is not null
        && provider.GlobalOptions.TryGetValue(Keys.ProjectProperties_EnableAotAnalyzer, out var value)
        && bool.TryParse(value, out var enabled)
        && enabled;

    public static bool TryGetSqlSyntax(this AnalyzerOptions? options, out SqlSyntax syntax)
    {
        if (options is not null)
        {
            if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Keys.GlobalOptions_DapperSqlSyntax, out var value)
                && Enum.TryParse<SqlSyntax>(value, true, out syntax))
            {
                return true;
            }
            if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Keys.ProjectProperties_DapperSqlSyntax, out value)
                && Enum.TryParse<SqlSyntax>(value, true, out syntax))
            {
                return true;
            }
        }
        syntax = SqlSyntax.General;
        return false;
    }

    public static bool TryGetDebugModeFlags(this AnalyzerOptions? options, out SqlParseInputFlags flags)
    {
        if (options is not null)
        {
            if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Keys.GlobalOptions_DapperDebugSqlParseInputFlags, out var value)
                && Enum.TryParse<SqlParseInputFlags>(value, true, out flags))
            {
                return true;
            }
        }
        flags = SqlParseInputFlags.None;
        return false;
    }
}

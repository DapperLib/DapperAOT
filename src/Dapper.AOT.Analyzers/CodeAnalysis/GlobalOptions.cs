using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using static Dapper.SqlAnalysis.TSqlProcessor;

namespace Dapper.CodeAnalysis;

internal static class GlobalOptions
{
    public static class Keys
    {
        public const string GlobalOptions_DapperSqlSyntax = "dapper.sqlsyntax";
        public const string GlobalOptions_DapperDebugModeFlags = "dapper.debug_mode_flags"; // this is for test purposes; if you find and use this: don't blame me!
        public const string ProjectProperties_DapperSqlSyntax = "build_property.Dapper_SqlSyntax";
        
    }

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

    public static bool TryGetDebugModeFlags(this AnalyzerOptions? options, out ModeFlags flags)
    {
        if (options is not null)
        {
            if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(Keys.GlobalOptions_DapperDebugModeFlags, out var value)
                && Enum.TryParse<ModeFlags>(value, true, out flags))
            {
                return true;
            }
        }
        flags = ModeFlags.None;
        return false;
    }
}

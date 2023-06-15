using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor
        UnsupportedMethod = new("DAP001", "Unsupported method",
            "The Dapper method '{0}' is not currently supported by Dapper.AOT", Category.Library, DiagnosticSeverity.Info, true),
        UntypedResults = new("DAP002", "Untyped result types",
            "Dapper.AOT does not currently support untyped/dynamic results", Category.Library, DiagnosticSeverity.Info, true),
        InterceptorsNotEnabled = new("DAP003", "Interceptors not enabled",
            "Interceptors are an experimental feature, and requires that '<Features>interceptors</Features>' be added to the project file", Category.Library, DiagnosticSeverity.Warning, true),
        LanguageVersionTooLow = new("DAP004", "Language version too low",
            "Interceptors require at least C# version 11", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotNotEnabled = new("DAP005", "Dapper.AOT not enabled",
            "Candidate Dapper methods were detected, but none have Dapper.AOT enabled; [DapperAot] can be added at the method, type, module or assembly level (for example '[module:DapperAot]')", Category.Library, DiagnosticSeverity.Info, true);

    internal static class Category
    {
        public const string Library = nameof(Library);
    }
}

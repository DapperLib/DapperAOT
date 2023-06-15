using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor
        UnsupportedMethod = new DiagnosticDescriptor("DAP001", "Unsupported method",
            "The Dapper method '{0}' is not currently supported by Dapper.AOT", Category.Library, DiagnosticSeverity.Info, true),
        UntypedResults = new DiagnosticDescriptor("DAP002", "Untyped result types",
            "Dapper.AOT does not currently support untyped/dynamic results", Category.Library, DiagnosticSeverity.Info, true);


    internal static class Category
    {
        public const string Library = nameof(Library);
    }
}

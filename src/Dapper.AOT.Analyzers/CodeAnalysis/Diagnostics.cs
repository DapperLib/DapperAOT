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
            "Candidate Dapper methods were detected, but none have Dapper.AOT enabled; [DapperAot] can be added at the method, type, module or assembly level (for example '[module:DapperAot]')", Category.Library, DiagnosticSeverity.Info, true),
        DapperLegacyTupleParameter = new("DAP006", "Dapper tuple-type parameter",
            "Dapper (original) does not work well with tuple-type parameters as name information is inaccessible", Category.Library, DiagnosticSeverity.Warning, true),
        UnexpectedCommandType = new("DAP007", "Unexpected command type",
            "The command type specified is not understood", Category.Library, DiagnosticSeverity.Info, true),
        NonConstantSql = new("DAP008", "Non-constant SQL",
            "Only constant SQL is currently supported", Category.Library, DiagnosticSeverity.Info, true),
        UnexpectedArgument = new("DAP009", "Unexpected parameter",
            "The parameter '{0}' is not understood", Category.Library, DiagnosticSeverity.Info, true),
        SqlNotDetected = new("DAP010", "Missing SQL",
            "The SQL for this operation could not be identified", Category.Library, DiagnosticSeverity.Info, true),
        DapperLegacyBindNameTupleResults = new("DAP011", "Named-tuple results",
            "Dapper (original) does not support tuple results with bind-by-name semantics", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotAddBindByName = new("DAP012", "Add BindByName",
            "Because of differences in how Dapper and Dapper.AOT can process tuple-types, please add '[BindByName({true|false})]' to clarify your intent", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotTupleResults = new("DAP013", "Tuple-type results",
            "Tuple-type results are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        DapperAotTupleParameter = new("DAP014", "Tuple-type parameter",
            "Tuple-type parameters are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        UntypedParameter = new("DAP015", "Untyped parameter",
            "The parameter type could not be resolved", Category.Library, DiagnosticSeverity.Info, true);

    internal static class Category
    {
        public const string Library = nameof(Library);
    }
}

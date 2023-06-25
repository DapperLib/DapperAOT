using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Dapper.CodeAnalysis;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor
        InterceptorsGenerated = new("DAP000", "Interceptors generated",
            "Dapper.AOT handled {0} of {1} enabled call-sites using {2} interceptors, {3} commands and {4} readers", Category.Library, DiagnosticSeverity.Hidden, true),
        UnsupportedMethod = new("DAP001", "Unsupported method",
            "The Dapper method '{0}' is not currently supported by Dapper.AOT", Category.Library, DiagnosticSeverity.Info, true),
        //UntypedResults = new("DAP002", "Untyped result types",
        //    "Dapper.AOT does not currently support untyped/dynamic results", Category.Library, DiagnosticSeverity.Info, true),
        InterceptorsNotEnabled = new("DAP003", "Interceptors not enabled",
            "Interceptors are an experimental feature, and requires that '<Features>InterceptorsPreview</Features>' be added to the project file", Category.Library, DiagnosticSeverity.Warning, true),
        LanguageVersionTooLow = new("DAP004", "Language version too low",
            "Interceptors require at least C# version 11", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotNotEnabled = new("DAP005", "Dapper.AOT not enabled",
            "Candidate Dapper methods were detected, but none have Dapper.AOT enabled; [DapperAot] can be added at the method, type, module or assembly level (for example '[module:DapperAot]')", Category.Library, DiagnosticSeverity.Info, true),
        DapperLegacyTupleParameter = new("DAP006", "Dapper tuple-type parameter",
            "Dapper (original) does not work well with tuple-type parameters as name information is inaccessible", Category.Library, DiagnosticSeverity.Warning, true),
        UnexpectedCommandType = new("DAP007", "Unexpected command type",
            "The command type specified is not understood", Category.Library, DiagnosticSeverity.Info, true),
        // space
        UnexpectedArgument = new("DAP009", "Unexpected parameter",
            "The parameter '{0}' is not understood", Category.Library, DiagnosticSeverity.Info, true),
        // space
        DapperLegacyBindNameTupleResults = new("DAP011", "Named-tuple results",
            "Dapper (original) does not support tuple results with bind-by-name semantics", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotAddBindTupleByName = new("DAP012", "Add BindTupleByName",
            "Because of differences in how Dapper and Dapper.AOT can process tuple-types, please add '[BindTupleByName({true|false})]' to clarify your intent", Category.Library, DiagnosticSeverity.Warning, true),
        DapperAotTupleResults = new("DAP013", "Tuple-type results",
            "Tuple-type results are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        DapperAotTupleParameter = new("DAP014", "Tuple-type parameter",
            "Tuple-type parameters are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        UntypedParameter = new("DAP015", "Untyped parameter",
            "The parameter type could not be resolved", Category.Library, DiagnosticSeverity.Info, true),
        GenericTypeParameter = new("DAP016", "Generic type parameter",
            "Generic type parameters ({0}) are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        NonPublicType = new("DAP017", "Non-accessible type",
            "Type '{0}' is not accessible; {1} types are not currently supported", Category.Library, DiagnosticSeverity.Info, true),
        SqlParametersNotDetected = new("DAP018", "SQL parameters not detected",
            "Parameters are being supplied, but no parameters were detected in the command", Category.Sql, DiagnosticSeverity.Warning, true),
        NoParametersSupplied = new("DAP019", "No parameters supplied",
            "SQL parameters were detected, but no parameters are being supplied", Category.Sql, DiagnosticSeverity.Error, true),
        SqlParameterNotBound = new("DAP020", "SQL parameter not bound",
            "No member could be found for the SQL parameter '{0}' from type '{1}'", Category.Sql, DiagnosticSeverity.Error, true),
        DuplicateParameter = new("DAP021", "Duplicate parameter",
            "Members '{0}' and '{1}' both have the database name '{2}'; '{0}' will be ignored", Category.Sql, DiagnosticSeverity.Warning, true),
        DuplicateReturn = new("DAP022", "Duplicate return parameter",
            "Members '{0}' and '{1}' are both designated as return values; '{0}' will be ignored", Category.Sql, DiagnosticSeverity.Warning, true),
        DuplicateRowCount = new("DAP023", "Duplicate row-count member",
            "Members '{0}' and '{1}' are both marked [RowCount]", Category.Sql, DiagnosticSeverity.Warning, true),
        RowCountDbValue = new("DAP024", "Member is both row-count and mapped value",
            "Member '{0}' is marked both [RowCount] and [DbValue]; [DbValue] will be ignored", Category.Sql, DiagnosticSeverity.Warning, true),
        ExecuteCommandWithQuery = new("DAP025", "Execute command with query",
            "The command has a query that will be ignored", Category.Sql, DiagnosticSeverity.Warning, true),
        QueryCommandMissingQuery = new("DAP026", "Query/scalar command lacks query",
            "The command lacks a query", Category.Sql, DiagnosticSeverity.Error, true),


    // SQL parse specific
        SqlError = new("DAP200", "SQL error",
            "SQL error: {0}", Category.Sql, DiagnosticSeverity.Warning, true),
        MultipleBatches = new("DAP201", "Multiple batches",
            "Multiple batches are not permitted (L{0} C{1})", Category.Sql, DiagnosticSeverity.Error, true),
        DuplicateVariableDeclaration = new("DAP202", "Duplicate variable declaration",
            "The variable {0} is declared multiple times (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        GlobalIdentity = new("DAP203", "Do not use @@identity",
            "@@identity should not be used; prefer SCOPE_IDENTITY() or OUTPUT INSERTED.yourid (L{0} C{1})", Category.Sql, DiagnosticSeverity.Error, true),
        SelectScopeIdentity = new("DAP204", "Prefer OUTPUT over SELECT",
            "Consider using OUTPUT INSERTED.yourid in the INSERT instead of SELECT SCOPE_IDENTITY() (L{0} C{1})", Category.Sql, DiagnosticSeverity.Info, true),
        NullLiteralComparison = new("DAP205", "Null comparison",
            "Literal null used in comparison; 'is null' or 'is not null' should be preferred (L{0} C{1})", Category.Sql, DiagnosticSeverity.Warning, true),
        ParseError = new("DAP206", "SQL parse error",
            "{0} (#{1} L{2} C{3})", Category.Sql, DiagnosticSeverity.Error, true),
        ScalarVariableUsedAsTable = new("DAP207", "Scalar used like table",
            "Scalar variable {0} is used like a table (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        TableVariableUsedAsScalar = new("DAP208", "Table used like scalar",
            "Table-variable {0} is used like a scalar (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        TableVariableAccessedBeforePopulate = new("DAP209", "Table used before populate",
            "Table-variable {0} is accessed before it populated (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        VariableAccessedBeforeAssignment = new("DAP210", "Variable used before assigned",
            "Variable {0} is accessed before it is assigned a value (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        VariableAccessedBeforeDeclaration = new("DAP211", "Variable used before declared",
            "Variable {0} is accessed before it is declared (L{1} C{2})", Category.Sql, DiagnosticSeverity.Error, true),
        ExecVariable = new("DAP212", "EXEC with composed SQL",
            "EXEC with composed SQL may be susceptible to SQL injection; consider EXEC sp_executesql, taking care to fully parameterize the composed query (L{0} C{1})", Category.Sql, DiagnosticSeverity.Warning, true),
        VariableValueNotConsumed = new("DAP213", "Variable used before declared",
            "Variable {0} has a value that is not consumed (L{1} C{2})", Category.Sql, DiagnosticSeverity.Warning, true);


    // be careful moving this because of static field initialization order
    internal static readonly ImmutableArray<DiagnosticDescriptor> All = typeof(Diagnostics)
        .GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        .Select(x => x.GetValue(null)).OfType<DiagnosticDescriptor>().ToImmutableArray();

    internal static class Category
    {
        public const string Library = nameof(Library);
        public const string Sql = nameof(Sql);
    }

    internal static void Add(ref object? diagnostics, Diagnostic diagnostic)
    {
        if (diagnostic is null) throw new ArgumentNullException(nameof(diagnostic));
        switch (diagnostics)
        {   // single
            case null:
                diagnostics = diagnostic;
                break;
            case Diagnostic d:
                diagnostics = new List<Diagnostic> { d, diagnostic };
                break;
            case IList<Diagnostic> list when !list.IsReadOnly:
                list.Add(diagnostic);
                break;
            case IEnumerable<Diagnostic> list:
                diagnostics = new List<Diagnostic>(list) { diagnostic };
                break;
            default:
                throw new ArgumentException(nameof(diagnostics));
        }
    }
}

using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class DapperInterceptorGenerator
{
    internal sealed class Diagnostics : DiagnosticsBase
    {

        internal static readonly DiagnosticDescriptor
        InterceptorsGenerated = LibraryHidden("DAP000", "Interceptors generated", "Dapper.AOT handled {0} of {1} enabled call-sites using {2} interceptors, {3} commands and {4} readers"),
        UnsupportedMethod = LibraryInfo("DAP001", "Unsupported method", "The Dapper method '{0}' is not currently supported by Dapper.AOT"),
        //UntypedResults = new("DAP002", "Untyped result types",
        //    "Dapper.AOT does not currently support untyped/dynamic results", Category.Library, DiagnosticSeverity.Info, true),
        InterceptorsNotEnabled = LibraryWarning("DAP003", "Interceptors not enabled",
            "Interceptors need to be enabled (see help-link)", true),
        LanguageVersionTooLow = LibraryWarning("DAP004", "Language version too low", "Interceptors require at least C# version 11", true),
        DapperAotNotEnabled = LibraryInfo("DAP005", "Dapper.AOT not enabled",
            "Candidate Dapper methods were detected, but none have Dapper.AOT enabled; [DapperAot] can be added at the method, type, module or assembly level (for example '[module:DapperAot]')", true),
        DapperLegacyTupleParameter = LibraryWarning("DAP006", "Dapper tuple-type parameter", "Dapper (original) does not work well with tuple-type parameters as name information is inaccessible"),
        UnexpectedCommandType = LibraryInfo("DAP007", "Unexpected command type", "The command type specified is not understood"),
        // space
        UnexpectedArgument = LibraryInfo("DAP009", "Unexpected parameter", "The parameter '{0}' is not understood"),
        // space
        DapperLegacyBindNameTupleResults = LibraryWarning("DAP011", "Named-tuple results", "Dapper (original) does not support tuple results with bind-by-name semantics"),
        DapperAotAddBindTupleByName = LibraryWarning("DAP012", "Add BindTupleByName", "Because of differences in how Dapper and Dapper.AOT can process tuple-types, please add '[BindTupleByName({true|false})]' to clarify your intent"),
        DapperAotTupleResults = LibraryInfo("DAP013", "Tuple-type results", "Tuple-type results are not currently supported"),
        DapperAotTupleParameter = LibraryInfo("DAP014", "Tuple-type parameter", "Tuple-type parameters are not currently supported"),
        UntypedParameter = LibraryInfo("DAP015", "Untyped parameter", "The parameter type could not be resolved"),
        GenericTypeParameter = LibraryInfo("DAP016", "Generic type parameter", "Generic type parameters ({0}) are not currently supported"),
        NonPublicType = LibraryInfo("DAP017", "Non-accessible type", "Type '{0}' is not accessible; {1} types are not currently supported"),
        SqlParametersNotDetected = SqlWarning("DAP018", "SQL parameters not detected", "Parameters are being supplied, but no parameters were detected in the command"),
        NoParametersSupplied = SqlWarning("DAP019", "No parameters supplied", "SQL parameters were detected, but no parameters are being supplied", true),
        SqlParameterNotBound = SqlWarning("DAP020", "SQL parameter not bound", "No member could be found for the SQL parameter '{0}' from type '{1}'"),
        DuplicateParameter = LibraryWarning("DAP021", "Duplicate parameter", "Members '{0}' and '{1}' both have the database name '{2}'; '{0}' will be ignored"),
        DuplicateReturn = LibraryWarning("DAP022", "Duplicate return parameter", "Members '{0}' and '{1}' are both designated as return values; '{0}' will be ignored"),
        DuplicateRowCount = LibraryWarning("DAP023", "Duplicate row-count member",
            "Members '{0}' and '{1}' are both marked [RowCount]"),
        RowCountDbValue = LibraryWarning("DAP024", "Member is both row-count and mapped value",
            "Member '{0}' is marked both [RowCount] and [DbValue]; [DbValue] will be ignored"),
        ExecuteCommandWithQuery = SqlWarning("DAP025", "Execute command with query", "The command has a query that will be ignored"),
        QueryCommandMissingQuery = SqlError("DAP026", "Query/scalar command lacks query", "The command lacks a query"),
        UseSingleRowQuery = PerformanceWarning("DAP027", "Use single-row query", "Use {0}() instead of Query(...).{1}()"),
        MethodRowCountHintRedundant = LibraryInfo("DAP029", "Method-level row-count hint redundant", "The [EstimatedRowCount] will be ignored due to parameter member '{0}'"),
        MethodRowCountHintInvalid = LibraryError("DAP030", "Method-level row-count hint invalid",
            "The [EstimatedRowCount] parameters are invalid; a positive integer must be supplied"),
        MemberRowCountHintInvalid = LibraryError("DAP031", "Member-level row-count hint invalid",
            "The [EstimatedRowCount] parameters are invalid; no parameter should be supplied"),
        MemberRowCountHintDuplicated = LibraryError("DAP032", "Member-level row-count hint duplicated",
            "Only a single member should be marked [EstimatedRowCount]"),
        CommandPropertyNotFound = LibraryWarning("DAP033", "Command property not found",
            "Command property {0}.{1} was not found or was not valid; attribute will be ignored"),
        CommandPropertyReserved = LibraryWarning("DAP034", "Command property reserved",
            "Command property {1} is reserved for internal usage; attribute will be ignored"),
        TooManyDapperAotEnabledConstructors = LibraryError("DAP035", "Too many Dapper.AOT enabled constructors",
            "Only one constructor can be Dapper.AOT enabled per type '{0}'"),
        TooManyStandardConstructors = LibraryError("DAP036", "Type has more than 1 constructor to choose for creating an instance",
            "Type has more than 1 constructor, please, either mark one constructor with [DapperAot] or reduce amount of constructors"),
        UserTypeNoSettableMembersFound = LibraryError("DAP037", "No settable members exist for user type",
            "Type '{0}' has no settable members (fields or properties)"),
        ValueTypeSingleFirstOrDefaultUsage = LibraryWarning("DAP038", "Value-type single row 'OrDefault' usage",
            "Type '{0}' is a value-type; it will not be trivial to identify missing rows from {1}");
    }
}

using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class DapperInterceptorGenerator
{
    internal sealed class Diagnostics : DiagnosticsBase
    {

        internal static readonly DiagnosticDescriptor
            InterceptorsGenerated = LibraryHidden("DAP000", "Interceptors generated", "Dapper.AOT handled {0} of {1} enabled call-sites using {2} interceptors, {3} commands and {4} readers"),
            //UntypedResults = new("DAP002", "Untyped result types",
            //    "Dapper.AOT does not currently support untyped/dynamic results", Category.Library, DiagnosticSeverity.Info, true),
            InterceptorsNotEnabled = LibraryWarning("DAP003", "Interceptors not enabled",
                "Interceptors need to be enabled", true),
            LanguageVersionTooLow = LibraryWarning("DAP004", "Language version too low", "Interceptors require at least C# version 11", true),


            CommandPropertyNotFound = LibraryWarning("DAP033", "Command property not found", "Command property {0}.{1} was not found or was not valid; attribute will be ignored"),
            CommandPropertyReserved = LibraryWarning("DAP034", "Command property reserved", "Command property {1} is reserved for internal usage; attribute will be ignored"),
            UserTypeNoSettableMembersFound = LibraryError("DAP037", "No settable members exist for user type", "Type '{0}' has no settable members (fields or properties)");
    }
}

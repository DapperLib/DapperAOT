using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class DapperInterceptorGenerator
{
    internal sealed class Diagnostics : DiagnosticsBase
    {

        internal static readonly DiagnosticDescriptor
            InterceptorsGenerated = LibraryHidden("DAP000", "Interceptors generated", "Dapper.AOT handled {0} of {1} possible call-sites using {2} interceptors, {3} commands and {4} readers"),
            InterceptorsNotEnabled = LibraryWarning("DAP003", "Interceptors not enabled",
                $"Interceptors need to be enabled by adding '<InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);{FeatureKeys.CodegenNamespace}</InterceptorsPreviewNamespaces>' to your project file"),
            LanguageVersionTooLow = LibraryWarning("DAP004", "Language version too low", "Interceptors require at least C# version 11"),


            CommandPropertyNotFound = LibraryWarning("DAP033", "Command property not found", "Command property {0}.{1} was not found or was not valid; attribute will be ignored"),
            CommandPropertyReserved = LibraryWarning("DAP034", "Command property reserved", "Command property {1} is reserved for internal usage; attribute will be ignored");
    }
}

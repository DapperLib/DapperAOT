using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class TypeAccessorInterceptorGenerator
{
    internal sealed class Diagnostics : DiagnosticsBase
    {
        internal static readonly DiagnosticDescriptor
            // TypeAccessor
            TypeAccessorCollectionTypeNotAllowed = new("DAP100", "TypeAccessors does not allow collection types",
                "TypeAccessors does not allow collection types", Category.Library, DiagnosticSeverity.Error, true),
            TypeAccessorPrimitiveTypeNotAllowed = new("DAP101", "TypeAccessors does not allow primitive types",
                "TypeAccessors does not allow primitive types", Category.Library, DiagnosticSeverity.Error, true),
            TypeAccessorMembersNotParsed = new("DAP102", "TypeAccessor members can not be parsed",
                "At least one gettable and settable member must be defined for type '{0}'", Category.Library, DiagnosticSeverity.Error, true);

    }
}

using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class TypeAccessorInterceptorGenerator
{
    internal sealed class Diagnostics : DiagnosticsBase
    {
        internal static readonly DiagnosticDescriptor
            // TypeAccessor
            TypeAccessorCollectionTypeNotAllowed = LibraryError("DAP100", "TypeAccessors does not allow collection types",
                "TypeAccessors does not allow collection types"),
            TypeAccessorPrimitiveTypeNotAllowed = LibraryError("DAP101", "TypeAccessors does not allow primitive types",
                "TypeAccessors does not allow primitive types"),
            TypeAccessorMembersNotParsed = LibraryError("DAP102", "TypeAccessor members can not be parsed",
                "At least one gettable and settable member must be defined for type '{0}'");

    }
}

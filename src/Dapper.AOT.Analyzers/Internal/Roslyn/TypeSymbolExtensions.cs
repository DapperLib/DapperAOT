using Microsoft.CodeAnalysis;
using Dapper.Internal.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dapper.Internal.Roslyn;

// TODO deaglegross: fix the public modifier
// probably need to sign test with sn.exe
public static class TypeSymbolExtensions
{
    public static bool InNamespace(this ITypeSymbol? typeSymbol, string @namespace)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.ContainingNamespace?.ToDisplayString() == @namespace;
    }

    public static string? GetContainingTypeFullName(this ITypeSymbol? typeSymbol, int typeArgIndex = 0) => typeSymbol.GetContainingTypeSymbol(typeArgIndex)?.ToDisplayString();
    public static ITypeSymbol? GetContainingTypeSymbol(this ITypeSymbol? typeSymbol, int typeArgIndex = 0)
    {
        if (typeSymbol is null) return null;
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            var typeArguments = namedTypeSymbol.TypeArguments;
            if (typeArgIndex >= typeArguments.Length) return null;
            return typeArguments[typeArgIndex];
        }
        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            return arrayTypeSymbol.ElementType;
        }

        return null;
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> represents <see cref="ImmutableArray{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool IsImmutableArray(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return false;

        return namedTypeSymbol.TypeKind == TypeKind.Struct
            && namedTypeSymbol.InNamespace("System.Collections.Immutable")
            && namedTypeSymbol.Arity == 1
            && namedTypeSymbol.Name == "ImmutableArray";
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> represents array.
    /// False otherwise
    /// </returns>
    public static bool IsArray(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        if (typeSymbol is not IArrayTypeSymbol arrayTypeSymbol) return false;

        return arrayTypeSymbol.TypeKind == TypeKind.Array
            && arrayTypeSymbol.BaseType?.InNamespace("System") == true
            && arrayTypeSymbol.BaseType?.Name == "Array";
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> represents <see cref="List{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool IsList(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) return false;

        return namedTypeSymbol.TypeKind == TypeKind.Class
            && namedTypeSymbol.InNamespace("System.Collections.Generic")
            && namedTypeSymbol.Arity == 1
            && namedTypeSymbol.Name == "List";
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IList{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsGenericIEnumerable(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        // note: IEnumerable<T> most probably is one of the last in interfaces implementation order:
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.itypesymbol.allinterfaces?view=roslyn-dotnet
        for (var i = typeSymbol.AllInterfaces.Length - 1; i >= 0; i--)
        {
            var interfaceSpecialType = typeSymbol.AllInterfaces[i].SpecialType;
            var originalSpecialType = typeSymbol.AllInterfaces[i].OriginalDefinition?.SpecialType;

            if (interfaceSpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
                || originalSpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return true;
            }
        }

        return false;
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IEnumerable"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsIEnumerable(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        // note: IEnumerable most probably is last in interfaces implementation order:
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.itypesymbol.allinterfaces?view=roslyn-dotnet
        for (var i = typeSymbol.AllInterfaces.Length - 1; i >= 0; i--)
        {
            var interfaceSpecialType = typeSymbol.AllInterfaces[i].SpecialType;
            if (interfaceSpecialType == SpecialType.System_Collections_IEnumerable) return true;
        }

        return false;
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IList{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsIList(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.AllInterfaces.Contains(
            x => x.SpecialType == SpecialType.System_Collections_Generic_IList_T
              || x.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_IList_T);
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="ICollection{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsICollection(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.AllInterfaces.Contains(
            x => x.SpecialType == SpecialType.System_Collections_Generic_ICollection_T
              || x.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_ICollection_T);
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IReadOnlyCollection{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsIReadOnlyCollection(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.AllInterfaces.Contains(
            x => x.SpecialType == SpecialType.System_Collections_Generic_IReadOnlyCollection_T
              || x.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_IReadOnlyCollection_T);
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IReadOnlyList{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool ImplementsIReadOnlyList(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.AllInterfaces.Contains(
            x => x.SpecialType == SpecialType.System_Collections_Generic_IReadOnlyList_T
              || x.OriginalDefinition?.SpecialType == SpecialType.System_Collections_Generic_IReadOnlyList_T);
    }
}
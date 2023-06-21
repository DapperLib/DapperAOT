﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Dapper.Internal.Roslyn;

internal static class TypeSymbolExtensions
{
    public static string? GetTypeDisplayName(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return null;
        if (typeSymbol.IsAnonymousType == true) return "object?";
        return CodeWriter.GetTypeName(typeSymbol);
    }
    public static string? GetContainingTypeDisplayName(this ITypeSymbol? typeSymbol, int typeArgIndex = 0)
    {
        var containingTypeSymbol = typeSymbol.GetContainingTypeSymbol(typeArgIndex);
        return containingTypeSymbol.GetTypeDisplayName();
    }
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
    /// True, if passed <param name="typeSymbol"/> represents array. False otherwise
    /// </returns>
    /// <remarks>Checks it type is a zero-based one-dimensional array</remarks>
    public static bool IsArray(this ITypeSymbol? typeSymbol) => typeSymbol is IArrayTypeSymbol { IsSZArray: true };

    /// <returns>
    /// True, if passed <param name="type"/> represents <see cref="List{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool IsList(this ITypeSymbol? type) => IsStandardCollection(type, "List");

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> represents <see cref="ImmutableArray{T}"/>.
    /// False otherwise
    /// </returns>
    public static bool IsImmutableArray(this ITypeSymbol? typeSymbol) => IsStandardCollection(typeSymbol, "ImmutableArray", "Immutable", TypeKind.Struct);

    private static bool IsStandardCollection(ITypeSymbol? type, string name, string nsName = "Generic", TypeKind kind = TypeKind.Class)
        => type is INamedTypeSymbol named
            && named.Name == name
            && named.TypeKind == kind
            && named.Arity == 1
            && named.ContainingSymbol is INamespaceSymbol ns
            && ns.Name == nsName
            && ns is
            {
                ContainingNamespace:
                {
                    Name: "Collections",
                    ContainingNamespace:
                    {
                        Name: "System",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            };

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IEnumerable{T}"/>. False otherwise
    /// </returns>
    /// <param name="searchedTypeSymbol">if found, an interface type symbol</param>
    /// <remarks>
    /// Most likely <see cref="IEnumerable{T}"/> is one of the last defined interfaces in a chain of implementations
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.itypesymbol.allinterfaces?view=roslyn-dotnet
    /// </remarks>
    public static bool ImplementsIEnumerable(this ITypeSymbol? typeSymbol, out ITypeSymbol? searchedTypeSymbol)
        => typeSymbol.ImplementsInterface(SpecialType.System_Collections_Generic_IEnumerable_T, out searchedTypeSymbol, searchFromStart: false);

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IList{T}"/>. False otherwise
    /// </returns>
    /// <param name="searchedTypeSymbol">if found, an interface type symbol</param>
    public static bool ImplementsIList(this ITypeSymbol? typeSymbol, out ITypeSymbol? searchedTypeSymbol)
        => typeSymbol.ImplementsInterface(SpecialType.System_Collections_Generic_IList_T, out searchedTypeSymbol);


    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="ICollection{T}"/>. False otherwise
    /// </returns>
    /// <param name="searchedTypeSymbol">if found, an interface type symbol</param>
    public static bool ImplementsICollection(this ITypeSymbol? typeSymbol, out ITypeSymbol? searchedTypeSymbol)
        => typeSymbol.ImplementsInterface(SpecialType.System_Collections_Generic_ICollection_T, out searchedTypeSymbol);

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IReadOnlyCollection{T}"/>. False otherwise
    /// </returns>
    /// <param name="searchedTypeSymbol">if found, an interface type symbol</param>
    public static bool ImplementsIReadOnlyCollection(this ITypeSymbol? typeSymbol, out ITypeSymbol? searchedTypeSymbol)
        => typeSymbol.ImplementsInterface(SpecialType.System_Collections_Generic_IReadOnlyCollection_T, out searchedTypeSymbol);

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> implements <see cref="IReadOnlyList{T}"/>. False otherwise
    /// </returns>
    /// <param name="searchedTypeSymbol">if found, an interface type symbol</param>
    public static bool ImplementsIReadOnlyList(this ITypeSymbol? typeSymbol, out ITypeSymbol? searchedTypeSymbol) 
        => typeSymbol.ImplementsInterface(SpecialType.System_Collections_Generic_IReadOnlyList_T, out searchedTypeSymbol);

    private static bool ImplementsInterface(
        this ITypeSymbol? typeSymbol,
        SpecialType interfaceType,
        out ITypeSymbol? searchedInterface,
        bool searchFromStart = true)
    {
        if (typeSymbol is null)
        {
            searchedInterface = null;
            return false;
        }

        if (searchFromStart)
        {
            for (var i = 0; i < typeSymbol.AllInterfaces.Length; i++)
            {
                var currentSymbol = typeSymbol.AllInterfaces[i];

                if (currentSymbol.SpecialType == interfaceType 
                    || currentSymbol.OriginalDefinition?.SpecialType == interfaceType)
                {
                    searchedInterface = currentSymbol;
                    return true;
                }
            }
        }
        else
        {
            for (var i = 0; i < typeSymbol.AllInterfaces.Length; i++)
            {
                var currentSymbol = typeSymbol.AllInterfaces[i];

                if (currentSymbol.SpecialType == interfaceType
                    || currentSymbol.OriginalDefinition?.SpecialType == interfaceType)
                {
                    searchedInterface = currentSymbol;
                    return true;
                }
            }
        }

        searchedInterface = null;
        return false;
    }
}
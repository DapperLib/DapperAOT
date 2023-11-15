using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Dapper.Internal.Roslyn;

internal static class TypeSymbolExtensions
{
    public static IEnumerable<IMethodSymbol>? GetMethods(
        this ITypeSymbol? typeSymbol,
        Func<IMethodSymbol, bool>? filter = null)
    {
        if (typeSymbol is null) yield break;
        
        foreach (var methodSymbol in typeSymbol.GetMembers()
                     .OfType<IMethodSymbol>()
                     .Where(m => m.MethodKind is MethodKind.Ordinary or MethodKind.DeclareMethod))
        {
            if (filter is null || filter(methodSymbol)) yield return methodSymbol;
        }
    }

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

    public static string? GetUnderlyingEnumTypeName(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum } namedTypeSymbol) return null;
        var enumUnderlyingType = namedTypeSymbol.EnumUnderlyingType;
        return enumUnderlyingType?.ToDisplayString();
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> is a primitive type like <see cref="int"/>, <see cref="bool"/>, <see cref="string"/>, <see cref="object"/>, etc.
    /// </returns>
    public static bool IsPrimitiveType(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Object or SpecialType.System_Enum or
            SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single or
            SpecialType.System_Double or SpecialType.System_String or SpecialType.System_IntPtr or SpecialType.System_UIntPtr 
              => true,
            _ => false
        };
    }

    public static bool IsNullable(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated) return true;
        return typeSymbol.TypeKind is 
            TypeKind.Class or TypeKind.Array or
            TypeKind.Delegate or TypeKind.Dynamic or
            TypeKind.Interface;
    }

    /// <returns>
    /// Returns true, if <paramref name="typeSymbol"/> represents an <see cref="object"/> type.
    /// </returns>
    public static bool IsSystemObject(this ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;
        return typeSymbol.SpecialType == SpecialType.System_Object;
    }

    /// <returns>
    /// True, if passed <param name="typeSymbol"/> represents array. False otherwise
    /// </returns>
    /// <remarks>Checks it type is a zero-based one-dimensional array</remarks>
    public static bool IsArray(this ITypeSymbol? typeSymbol) => typeSymbol is IArrayTypeSymbol { IsSZArray: true };

    public static bool IsAsync(this ITypeSymbol? type, out ITypeSymbol? result)
    {
        if (type is not INamedTypeSymbol { Arity: <= 1 } named)
        {
            result = null;
            return false;
        }
        result = named.Arity == 0 ? null : named.TypeArguments[0];
        if (named.Name is "Task" or "ValueTask"
            && named.ContainingType is null
            && named.ContainingNamespace is
            {
                Name: "Tasks",
                ContainingNamespace:
                {
                    Name: "Threading",
                    ContainingNamespace:
                    {
                        Name: "System",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            })
        {
            return true;
        }
        if (IsStandardCollection(type, "IAsyncEnumerable", kind: TypeKind.Interface))
        {
            return true;
        }
        result = null;
        return false;
    }

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
    /// https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.itypesymbol.allinterfaces?view=roslyn-dotnet
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
        out ITypeSymbol? found,
        bool searchFromStart = true)
    {
        if (typeSymbol is null)
        {
            found = null;
            return false;
        }
        if (typeSymbol.SpecialType == interfaceType || typeSymbol.OriginalDefinition?.SpecialType == interfaceType)
        {
            found = typeSymbol;
            return true;
        }

        if (searchFromStart)
        {
            for (var i = 0; i < typeSymbol.AllInterfaces.Length; i++)
            {
                var currentSymbol = typeSymbol.AllInterfaces[i];

                if (currentSymbol.SpecialType == interfaceType 
                    || currentSymbol.OriginalDefinition?.SpecialType == interfaceType)
                {
                    found = currentSymbol;
                    return true;
                }
            }
        }
        else
        {
            for (var i = typeSymbol.AllInterfaces.Length - 1; i >= 0; i--)
            {
                var currentSymbol = typeSymbol.AllInterfaces[i];

                if (currentSymbol.SpecialType == interfaceType
                    || currentSymbol.OriginalDefinition?.SpecialType == interfaceType)
                {
                    found = currentSymbol;
                    return true;
                }
            }
        }

        found = null;
        return false;
    }
}
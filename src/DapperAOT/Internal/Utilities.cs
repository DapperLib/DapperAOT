using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace DapperAOT.Internal
{
    internal static class Utilities
    {
        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string name)
            => type?.Name == name
            && type.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string name)
            => type?.Name == name
            && type.ContainingNamespace?.Name == ns1
            && type.ContainingNamespace.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string ns2, string name)
            => type?.Name == name
            && type.ContainingNamespace?.Name == ns2
            && type.ContainingNamespace.ContainingNamespace?.Name == ns1
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string name)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, name)) return true;
                type = type.BaseType;
            }
            return false;
        }
        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string name)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, ns1, name)) return true;
                type = type.BaseType;
            }
            return false;
        }
        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string ns2, string name)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, ns1, ns2, name)) return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}

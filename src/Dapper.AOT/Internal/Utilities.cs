using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Dapper.Internal
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
        public static bool IsDefined(this IMethodSymbol symbol, ITypeSymbol attributeType)
        {
            foreach (var attrib in symbol.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attrib.AttributeClass, attributeType))
                    return true;
            }
            return false;
        }

        public static bool TryGetAttributeValue<T>(this AttributeData? attrib, string name, [NotNullWhen(true)] out T? value)
        {
            if (attrib is not null)
            {
                // check named values first, since they take precedence semantically
                foreach (var na in attrib.NamedArguments)
                {
                    if (string.Equals(na.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (na.Value.Value is T typedNamedArgValue)
                        {
                            value = typedNamedArgValue;
                            return true;
                        }
                        break;
                    }
                }


                // check parameter values second
                var ctor = attrib.AttributeConstructor;
                var index = FindParameterIndex(ctor, name);
                if (index >= 0 && index < attrib.ConstructorArguments.Length && attrib.ConstructorArguments[index].Value is T typedCtorValue)
                {
                    value = typedCtorValue;
                    return true;
                }

                static int FindParameterIndex(IMethodSymbol? method, string name)
                {
                    if (method is not null)
                    {
                        int index = 0;
                        foreach (var p in method.Parameters)
                        {
                            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                                return index;
                            index++;
                        }
                    }
                    return -1;
                }
            }
            value = default;
            return false;
        }
    }
}

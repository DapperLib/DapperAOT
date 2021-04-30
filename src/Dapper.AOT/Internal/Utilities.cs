using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

        public static (QueryFlags Flags, ITypeSymbol? ElementType, NullableAnnotation ElementNullable) CategorizeQuery(IMethodSymbol method)
		{
            // detect void; non-query
            var retType = method.ReturnType;
            if (retType.SpecialType == SpecialType.System_Void)
            {
                return (QueryFlags.None, null, NullableAnnotation.None);
            }

            if (retType is INamedTypeSymbol named)
            {
                switch (named.Arity)
				{
                    case 0:
                        // detect non-typed Task/ValueTask; async non-query
                        if (named.IsExact("System", "Threading", "Tasks", "Task") || named.IsExact("System", "Threading", "Tasks", "ValueTask"))
                        {
                            return (QueryFlags.IsAsync, null, NullableAnnotation.None);
                        }
                        break;
                    case 1:
                        // detect non-async iterator queries
                        if (named.IsExact("System", "Collections", "Generic", "IEnumerable") || named.IsExact("System", "Collections", "Generic", "IEnumerator"))
                        {
                            return (QueryFlags.IsQuery | QueryFlags.IsIterator, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0]);
                        }
                        // detect async iterator queries
                        if (named.IsExact("System", "Collections", "Generic", "IAsyncEnumerable") || named.IsExact("System", "Collections", "Generic", "IAsyncEnumerator"))
                        {
                            return (QueryFlags.IsQuery | QueryFlags.IsIterator | QueryFlags.IsAsync, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0]);
                        }
                        // detect Task<T>, which could be Task<SomeRow> or Task<List<SomeRow>> etc
                        if (named.IsExact("System", "Threading", "Tasks", "Task") || named.IsExact("System", "Threading", "Tasks", "ValueTask"))
                        {
                            var t = named.TypeArguments[0];
                            // detect Task<List<SomeRow>>
                            if (IsCollectionType(t) && t is INamedTypeSymbol nt && nt.Arity == 1)
							{
                                return (QueryFlags.IsQuery, nt.TypeArguments[0], nt.TypeArgumentNullableAnnotations[0]);
                            }
                            else // Task<SomeRow> (note: could be return value)
							{
                                if (IsReturn(method.GetReturnTypeAttributes()))
                                {
                                    return (QueryFlags.IsAsync, t, named.TypeArgumentNullableAnnotations[0]);
                                }
                                return (GetSingleRowFlags(method) | QueryFlags.IsAsync, t, named.TypeArgumentNullableAnnotations[0]);
                            }
                        }

                        // detect List<T> etc queries
                        if (IsCollectionType(named))
						{
                            return (QueryFlags.IsQuery, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0]);
                        }
                        break;
                }
            }

            // just plain T (note: could be return value)
            if (IsReturn(method.GetReturnTypeAttributes()))
			{
                return (QueryFlags.None, method.ReturnType, method.ReturnNullableAnnotation);
			}
            return (GetSingleRowFlags(method), retType, method.ReturnNullableAnnotation);

            static bool IsCollectionType(ITypeSymbol type)
            => false; // not implemented yet; needs to detect arrays and anything obviously list type;
                      // maybe anything that : IEnumerable<T> **and** has a public Add(T) method is enough?
                      // note: what about immutable collection types?

            static bool IsReturn(ImmutableArray<AttributeData> attributes)
			{
                foreach (var attrib in attributes)
				{
                    if (attrib.AttributeClass.IsExact("Dapper", "ParameterAttribute"))
					{
                        return attrib.TryGetAttributeValue("Direction", out int direction)
                            && direction == (int)ParameterDirection.ReturnValue;
                    }
                }
                return false;
			}
            
            static QueryFlags GetSingleRowFlags(IMethodSymbol method)
			{
                const QueryFlags SingleRow = QueryFlags.IsQuery | QueryFlags.IsSingle;
                foreach (var attrib in method.GetAttributes())
                {
                    if (attrib.AttributeClass.IsExact("Dapper", "SingleRowAttribute"))
                    {
                        if (attrib.TryGetAttributeValue("Kind", out int kind))
                        {
                            switch (kind)
							{
                                case 0: return SingleRow; // FirstOrDefault
                                case 1: return SingleRow | QueryFlags.DemandAtLeastOneRow; // First
                                case 2: return SingleRow | QueryFlags.DemandAtMostOneRow; // SingleOrDefault
                                case 3: return SingleRow | QueryFlags.DemandAtLeastOneRow | QueryFlags.DemandAtMostOneRow; // Single
                                case 4: return QueryFlags.IsScalar; // Scalar
                            }
                        }
                        break; // only one such expected
                    }
                }
                // TODO: check for automatic scalars?
                return SingleRow;
            }
        }

        public static bool HasBasicAsyncMethod(this ITypeSymbol type, string methodName, [NotNullWhen(true)] out ITypeSymbol? returnType)
		{
            // looking by shape
            foreach (var member in type.GetMembers(methodName))
            {
                if (member is IMethodSymbol method && method.Parameters.IsDefaultOrEmpty
                    && !method.IsStatic && method.Arity == 0)
                {
                    returnType = method.ReturnType;
                    return returnType.IsExact("System", "Threading", "Tasks", "ValueTask")
                        || returnType.IsExact("System", "Threading", "Tasks", "Task");
                }
            }
            returnType = null;
            return false;
        }
        public static bool HasDisposeAsync(this ITypeSymbol type)
            => HasBasicAsyncMethod(type, nameof(IAsyncDisposable.DisposeAsync), out _);

        public static bool Has(this QueryFlags value, QueryFlags flag)
            => (value & flag) != 0;


        [Flags]
        public enum QueryFlags
		{
            None = 0,
            IsAsync = 1 << 0,
            IsQuery = 1 << 1,
            IsSingle = 1 << 2,
            IsIterator = 1 << 3,
            IsScalar = 1 << 4,
            DemandAtLeastOneRow = 1 << 5,
            DemandAtMostOneRow = 1 << 6,
        }
    }
}

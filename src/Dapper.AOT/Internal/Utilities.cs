using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace Dapper.Internal
{
    internal static class Utilities
    {
        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string name, int arity)
            => type is INamedTypeSymbol nt && nt.Name == name && nt.Arity == arity
            && type.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string name, int arity)
            => type is INamedTypeSymbol nt && nt.Name == name && nt.Arity == arity
            && type.ContainingNamespace?.Name == ns1
            && type.ContainingNamespace.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsExact([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string ns2, string name, int arity)
            => type is INamedTypeSymbol nt && nt.Name == name && nt.Arity == arity
            && type.ContainingNamespace?.Name == ns2
            && type.ContainingNamespace.ContainingNamespace?.Name == ns1
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name == ns0
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace;

        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string name, int arity)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, name, arity)) return true;
                foreach (var iType in type.Interfaces)
                {
                    if (iType.IsExact(ns0, name, arity)) return true;
                }
                type = type.BaseType;
            }
            return false;
        }
        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string name, int arity)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, ns1, name, arity)) return true;
                foreach (var iType in type.Interfaces)
                {
                    if (iType.IsExact(ns0, ns1, name, arity)) return true;
                }
                type = type.BaseType;
            }
            return false;
        }
        public static bool IsKindOf([NotNullWhen(true)] this ITypeSymbol? type, string ns0, string ns1, string ns2, string name, int arity)
        {
            while (type is not null)
            {
                if (type.IsExact(ns0, ns1, ns2, name, arity)) return true;
                foreach (var iType in type.Interfaces)
                {
                    if (iType.IsExact(ns0, ns1, ns2, name, arity)) return true;
                }
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

        public enum ListStrategy
        {
            None,
            Yield,
            SimpleList,
            Array,
            ImmutableArray,
            ImmutableList,
        }

        public readonly struct QueryCategory
        {
            public readonly QueryFlags Flags;
            public readonly ITypeSymbol? ItemType;
            public readonly NullableAnnotation ItemNullability;
            public readonly ListStrategy ListStrategy;
            public readonly ITypeSymbol ListType;

            public QueryCategory(
                QueryFlags flags, ITypeSymbol? itemType, NullableAnnotation itemNullability,
                ListStrategy listStrategy, ITypeSymbol listType)
            {
                Flags = flags;
                ItemType = itemType;
                ItemNullability = itemNullability;
                ListStrategy = listStrategy;
                ListType = listType;
            }

            public bool IsAsync() => Flags.IsAsync();
            public bool Has(QueryFlags flag) => Flags.Has(flag);
            public bool NeedsListLocal() => ListStrategy switch
            {
                ListStrategy.None => false,
                ListStrategy.Yield => false,
                _ => true,
            };
            public bool UseCollector() => ListStrategy switch
            {
                ListStrategy.Array => true,
                ListStrategy.ImmutableArray => true,
                ListStrategy.ImmutableList => true,
                _ => false,
            };

            internal bool HasListMethod(string methodName)
            {
                foreach (var member in ListType.GetMembers())
                {
                    if (member is IMethodSymbol method && !method.IsStatic
                        && method.Arity == 0 && method.Name == methodName
                        && method.Parameters.IsDefaultOrEmpty)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool IsCollectionType(ITypeSymbol returnType, in GeneratorExecutionContext context, out ListStrategy strategy, out ITypeSymbol listType)
            => (strategy = IsCollectionType(returnType, context, out listType)) != ListStrategy.None;

        private static ListStrategy IsCollectionType(ITypeSymbol returnType, in GeneratorExecutionContext context, out ITypeSymbol listType)
        {
            listType = returnType;
            if (returnType is INamedTypeSymbol nt)
            {
                static ITypeSymbol GetCollectorType(INamedTypeSymbol type, in GeneratorExecutionContext context)
                    => context.Compilation.GetTypeByMetadataName("Dapper.Internal.Collector`1")?.Construct(
                        type.TypeArguments, type.TypeArgumentNullableAnnotations) ?? type;

                if (nt.TypeKind == TypeKind.Array)
                {
                    listType = GetCollectorType(nt, context);
                    return ListStrategy.Array;
                }
                if (nt.TypeKind == TypeKind.Interface && nt.Arity == 1)
                {
                    if (returnType.IsExact("System", "Collections", "Immutable", "IImmutableList", 1))
                    {
                        listType = GetCollectorType(nt, context);
                        return ListStrategy.ImmutableList;
                    }
                    listType = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")?.Construct(
                        nt.TypeArguments, nt.TypeArgumentNullableAnnotations) ?? returnType;
                    return ListStrategy.SimpleList;
                }
                if (nt.IsExact("System", "Collections", "Immutable", "ImmutableArray", 1))
                {
                    listType = GetCollectorType(nt, context);
                    return ListStrategy.ImmutableArray;
                }
                if (returnType.IsExact("System", "Collections", "Immutable", "ImmutableList", 1))
                {
                    listType = GetCollectorType(nt, context);
                    return ListStrategy.ImmutableList;
                }
            }
            return ListStrategy.SimpleList;
        }

        public static QueryCategory CategorizeQuery(IMethodSymbol method, in GeneratorExecutionContext context)
		{
            // detect void; non-query
            var retType = method.ReturnType;
            if (retType.SpecialType == SpecialType.System_Void)
            {
                return new (QueryFlags.None, null, NullableAnnotation.None, ListStrategy.None, retType);
            }

            if (retType is INamedTypeSymbol named)
            {
                switch (named.Arity)
				{
                    case 0:
                        // detect non-typed Task/ValueTask; async non-query
                        if (named.IsExact("System", "Threading", "Tasks", "Task", 0) || named.IsExact("System", "Threading", "Tasks", "ValueTask", 0))
                        {
                            return new (QueryFlags.IsAsync, null, NullableAnnotation.None, ListStrategy.None, retType);
                        }
                        break;
                    case 1:
                        // detect non-async iterator queries
                        if (named.IsExact("System", "Collections", "Generic", "IEnumerable", 1) || named.IsExact("System", "Collections", "Generic", "IEnumerator", 1))
                        {
                            return new (QueryFlags.IsQuery | QueryFlags.IsIterator, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0], ListStrategy.Yield, retType);
                        }
                        // detect async iterator queries
                        if (named.IsExact("System", "Collections", "Generic", "IAsyncEnumerable", 1) || named.IsExact("System", "Collections", "Generic", "IAsyncEnumerator", 1))
                        {
                            return new (QueryFlags.IsQuery | QueryFlags.IsIterator | QueryFlags.IsAsync, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0], ListStrategy.Yield, retType);
                        }

                        ListStrategy strategy;
                        ITypeSymbol listType;
                        // detect Task<T>, which could be Task<SomeRow> or Task<List<SomeRow>> etc
                        if (named.IsExact("System", "Threading", "Tasks", "Task", 1) || named.IsExact("System", "Threading", "Tasks", "ValueTask", 1))
                        {
                            var t = named.TypeArguments[0];
                            // detect Task<List<SomeRow>>
                            if (IsCollectionType(t, context, out strategy, out listType) && t is INamedTypeSymbol nt && nt.Arity == 1)
							{
                                return new (QueryFlags.IsQuery, nt.TypeArguments[0], nt.TypeArgumentNullableAnnotations[0], strategy, listType);
                            }
                            else // Task<SomeRow> (note: could be return value)
							{
                                if (IsReturn(method.GetReturnTypeAttributes()))
                                {
                                    return new (QueryFlags.IsAsync, t, named.TypeArgumentNullableAnnotations[0], ListStrategy.None, retType);
                                }
                                return new (GetSingleRowFlags(method) | QueryFlags.IsAsync, t, named.TypeArgumentNullableAnnotations[0], ListStrategy.None, retType);
                            }
                        }

                        // detect List<T> etc queries
                        if (IsCollectionType(named, context, out strategy, out listType))
						{
                            return new (QueryFlags.IsQuery, named.TypeArguments[0], named.TypeArgumentNullableAnnotations[0], strategy, listType);
                        }
                        break;
                }
            }

            // just plain T (note: could be return value)
            if (IsReturn(method.GetReturnTypeAttributes()))
			{
                return new (QueryFlags.None, method.ReturnType, method.ReturnNullableAnnotation, ListStrategy.None, retType);
            }
            return new (GetSingleRowFlags(method), retType, method.ReturnNullableAnnotation, ListStrategy.None, retType);

            
            static bool IsReturn(ImmutableArray<AttributeData> attributes)
			{
                foreach (var attrib in attributes)
				{
                    if (attrib.AttributeClass.IsExact("Dapper", "ParameterAttribute", 0))
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
                    if (attrib.AttributeClass.IsExact("Dapper", "SingleRowAttribute", 0))
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

        public static bool IsEnumeratorCancellationToken(this IParameterSymbol parameter)
        {
            if (parameter.Type.IsExact("System", "Threading", "CancellationToken", 0))
            {
                foreach (var attrib in parameter.GetAttributes())
                {
                    if (attrib.AttributeClass.IsExact("System", "Runtime", "CompilerServices", "EnumeratorCancellationAttribute", 0))
                        return true;
                }
            }
            return false;
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
                    return returnType.IsExact("System", "Threading", "Tasks", "ValueTask", 0)
                        || returnType.IsExact("System", "Threading", "Tasks", "Task", 0);
                }
            }
            returnType = null;
            return false;
        }
        public static bool HasDisposeAsync(this ITypeSymbol type)
            => HasBasicAsyncMethod(type, nameof(IAsyncDisposable.DisposeAsync), out _);

        public static bool Has(this QueryFlags value, QueryFlags flag)
            => (value & flag) != 0;
        public static bool IsAsync(this QueryFlags value)
            => (value & QueryFlags.IsAsync) != 0;

        public static bool AllowUnsafe(this in GeneratorExecutionContext context)
            => context.Compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;


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

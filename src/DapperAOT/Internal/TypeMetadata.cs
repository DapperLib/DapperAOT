//using Microsoft.CodeAnalysis;
//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics.CodeAnalysis;

//namespace DapperAOT.Internal
//{
//    internal readonly struct TypeMetadata
//    {
//        public override bool Equals(object? obj)
//            => obj is TypeMetadata other && SymbolEqualityComparer.Default.Equals(OriginalType, other.OriginalType);
//        public override int GetHashCode()
//            => OriginalType?.GetHashCode() ?? 0;

//        public override string ToString()
//            => $"async: {AsyncKind}, cardinality: {Cardinality}, nullable: {NullableAnnotation}, type: {Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "void"}";

//        public AsyncKind AsyncKind { get; }
//        public NullableAnnotation NullableAnnotation { get; }
//        public bool IsAsync => AsyncKind != AsyncKind.None;
//        public ITypeSymbol OriginalType { get; }
//        public ITypeSymbol? Type { get; }
//        public Cardinality Cardinality { get; }
//        //public bool IncludeNullableAnnotation => NullableAnnotation == NullableAnnotation.Annotated;
//        //public bool CanBeNull => NullableAnnotation == NullableAnnotation.Annotated || ty

//        private static readonly ConcurrentDictionary<(ITypeSymbol, NullableAnnotation), TypeMetadata> s_Known = new();

//        public static TypeMetadata Get(IParameterSymbol parameter)
//            => Get(parameter.Type, parameter.NullableAnnotation);
//        public static TypeMetadata Get(IMethodSymbol method)
//            => Get(method.ReturnType, method.ReturnNullableAnnotation);

//        private static TypeMetadata Get(ITypeSymbol type, NullableAnnotation nullable)
//        {
//            if (type == null) throw new ArgumentNullException(nameof(type));
//            var key = (type, nullable);
//            if (!s_Known.TryGetValue(key, out var value))
//            {
//                s_Known[key] = value = new TypeMetadata(type, default);
//            }
//            return value;
//        }
//        public TypeMetadata(ITypeSymbol type, NullableAnnotation nullableAnnotation)
//        {
//            OriginalType = type;

//            ITypeSymbol? t;
//            NullableAnnotation tn;
//            if (type.IsExact("System", "Collections", "Generic", "IAsyncEnumerable") && IsT(type, out t, out tn))
//            {
//                AsyncKind = AsyncKind.ValueTask;
//                Cardinality = Cardinality.Sequence;
//                NullableAnnotation = ResolveNullable(ref t) ? NullableAnnotation.Annotated : tn;
//                Type = t;
//                return;
//            }
//            else if (type.IsExact("System", "Collections", "Generic", "IEnumerable") && IsT(type, out t, out tn))
//            {
//                AsyncKind = AsyncKind.None;
//                Cardinality = Cardinality.Sequence;
//                NullableAnnotation = ResolveNullable(ref t) ? NullableAnnotation.Annotated : tn;
//                Type = t;
//                return;
//            }

//            if (type.IsExact("System", "Threading", "Task"))
//            {
//                AsyncKind = AsyncKind.Task;
//                if (IsT(type, out t, out tn))
//                {
//                    type = t;
//                    nullableAnnotation = tn;
//                }
//            }
//            else if (type.IsExact("System", "Threading", "ValueTask"))
//            {
//                AsyncKind = AsyncKind.ValueTask;
//                if (IsT(type, out t, out tn))
//                {
//                    type = t;
//                    nullableAnnotation = tn;
//                }
//            }
//            else
//            {
//                AsyncKind = AsyncKind.None;
//            }

//            NullableAnnotation = nullableAnnotation;

//            if (ResolveNullable(ref type))
//                NullableAnnotation = NullableAnnotation.Annotated;

//            Type = type;
//            Cardinality = Cardinality.Simple;
//            if (type.TypeKind == TypeKind.Array)
//            {
//                Cardinality = Cardinality.Array;
//            }
//            else if (type.IsKindOf("System", "Collections", "Generic", "List") && IsT(type, out t, out tn))
//            {
//                Type = t;
//                NullableAnnotation = tn;
//                Cardinality = Cardinality.List;
//            }

//            static bool ResolveNullable(ref ITypeSymbol type)
//            {

//                if (type.IsValueType && type.IsExact("System", "Nullable") && IsT(type, out var t, out _))
//                {
//                    type = t;
//                    return true;
//                }
//                return false;
//            }

//            static bool IsT(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? t, out NullableAnnotation nullableAnnotation)
//            {
//                if (type is INamedTypeSymbol named && named.IsGenericType
//                    && named.TypeArguments.Length == 1)
//                {
//                    t = named.TypeArguments[0];
//                    nullableAnnotation = named.TypeArgumentNullableAnnotations[0];
//                    return true;
//                }
//                t = default;
//                nullableAnnotation = default;
//                return false;
//            }
//        }
//    }

//    internal enum Cardinality
//    {
//        Simple,
//        Sequence,
//        List,
//        Array,
//    }
//    internal enum AsyncKind
//    {
//        None,
//        Task,
//        ValueTask,
//        AsyncEnumerable
//    }

//}

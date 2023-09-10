﻿using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dapper.Internal;

internal static class Inspection
{
    public static bool InvolvesTupleType(this ITypeSymbol? type, out bool hasNames)
    {
        while (type is not null) // dive for inheritance
        {
            var named = type as INamedTypeSymbol;
            if (type.IsTupleType)
            {
                hasNames = false;
                if (named is not null)
                {
                    foreach (var field in named.TupleElements)
                    {
                        if (!string.IsNullOrWhiteSpace(field.Name))
                        {
                            hasNames = true;
                            break;
                        }
                    }
                    return true;
                }
            }
            if (type is IArrayTypeSymbol array)
            {
                return array.ElementType.InvolvesTupleType(out hasNames);
            }

            if (named is { IsGenericType: true })
            {
                var args = named.TypeArguments;
                foreach (var arg in args)
                {
                    if (arg.InvolvesTupleType(out hasNames)) return true;
                }
            }

            type = type.BaseType;
        }
        return hasNames = false;
    }
    public static AttributeData? GetClosestDapperAttribute(in GeneratorSyntaxContext ctx, IOperation op, string attributeName, CancellationToken cancellationToken)
        => GetClosestDapperAttribute(ctx, op, attributeName, out _, cancellationToken);
    public static AttributeData? GetClosestDapperAttribute(in GeneratorSyntaxContext ctx, IOperation op, string attributeName, out Location? location, CancellationToken cancellationToken)
    {
        var symbol = GetSymbol(ctx, op, cancellationToken);
        while (symbol is not null)
        {
            var attrib = GetDapperAttribute(symbol, attributeName);
            if (attrib is not null)
            {
                location = symbol.Locations.FirstOrDefault();
                return attrib;
            }
            symbol = symbol is IAssemblySymbol ? null : symbol.ContainingSymbol;
        }
        location = null;
        return null;
    }

    public static ISymbol? GetSymbol(in GeneratorSyntaxContext ctx, IOperation operation, CancellationToken cancellationToken)
    {
        var method = GetContainingMethodSyntax(operation);
        return method is null ? null : ctx.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
        static SyntaxNode? GetContainingMethodSyntax(IOperation op)
        {
            var syntax = op.Syntax;
            while (syntax is not null)
            {
                if (syntax.IsKind(SyntaxKind.MethodDeclaration))
                {
                    return syntax;
                }
                syntax = syntax.Parent;
            }
            return null;
        }
    }

    // support the fact that [DapperAot(bool)] can enable/disable generation at any level
    // including method, type, module and assembly; first attribute found (walking up the tree): wins
    public static bool IsEnabled(in GeneratorSyntaxContext ctx, IOperation op, string attributeName, out bool exists, CancellationToken cancellationToken)
    {
        var attrib = GetClosestDapperAttribute(ctx, op, attributeName, cancellationToken);
        if (attrib is not null && attrib.ConstructorArguments.Length == 1
            && attrib.ConstructorArguments[0].Value is bool b)
        {
            exists = true;
            return b;
        }
        exists = false;
        return false;
    }

    public static bool IsDapperAttribute(AttributeData attrib)
        => attrib.AttributeClass is
        {
            ContainingNamespace:
            {
                Name: "Dapper",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };

    public static bool HasDapperAotEnabledAttribute(ISymbol? symbol)
    {
        var dapperAotAttribute = GetDapperAttribute(symbol, Types.DapperAotAttribute);
        
        // no attribute at all
        if (dapperAotAttribute is null) return false;

        // `[DapperAot]`
        if (dapperAotAttribute.ConstructorArguments.Length == 0) return true;
        
        // `[DapperAot(true)]`
        var typedArg = dapperAotAttribute.ConstructorArguments.First();
        return (typedArg.Value is true);
    }

    public static AttributeData? GetDapperAttribute(ISymbol? symbol, string attributeName)
    {
        if (symbol is not null)
        {
            foreach (var attrib in symbol.GetAttributes())
            {
                if (IsDapperAttribute(attrib) && attrib.AttributeClass!.Name == attributeName)
                {
                    return attrib;
                }
            }
        }
        return null;
    }

    public static bool IsMissingOrObjectOrDynamic(ITypeSymbol? type) => type is null || type.SpecialType == SpecialType.System_Object || type.TypeKind == TypeKind.Dynamic;

    public static bool IsPublicOrAssemblyLocal(ISymbol? symbol, in GeneratorSyntaxContext ctx, out ISymbol? failingSymbol)
        => IsPublicOrAssemblyLocal(symbol, ctx.SemanticModel.Compilation.Assembly, out failingSymbol);

    public static bool IsPublicOrAssemblyLocal(ISymbol? symbol, IAssemblySymbol? assembly, out ISymbol? failingSymbol)
    {
        if (symbol is null || symbol.Kind == SymbolKind.DynamicType)
        {   // interpret null as "dynamic"
            failingSymbol = null;
            return true;
        }
        while (symbol is not null)
        {
            if (symbol is IArrayTypeSymbol array)
            {
                return IsPublicOrAssemblyLocal(array.ElementType, assembly, out failingSymbol);
            }
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    break; // fine, keep looking upwards
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal when assembly is not null:
                    if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, assembly))
                    {
                        // different assembly
                        failingSymbol = symbol;
                        return false;
                    }
                    break; // otherwise fine, keep looking upwards
                default:
                    failingSymbol = symbol;
                    return false;
            }

            symbol = symbol.ContainingType;
        }
        failingSymbol = null;
        return true;
    }

    public static bool InvolvesGenericTypeParameter(ISymbol? symbol)
    {
        while (symbol is not null)
        {
            if (symbol is ITypeParameterSymbol)
            {
                return true;
            }

            if (symbol is INamedTypeSymbol named && named.Arity != 0)
            {
                foreach (var arg in named.TypeArguments)
                {
                    if (InvolvesGenericTypeParameter(arg))
                    {
                        return true;
                    }
                }
            }
            // could be Something.Foo<T>.SomethingElse
            symbol = symbol.ContainingType;
        }

        return false;
    }

    public static string NameAccessibility(ISymbol symbol)
    {
        var accessibility = symbol is IArrayTypeSymbol array
            ? array.ElementType.DeclaredAccessibility : symbol.DeclaredAccessibility;
        return accessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => "non-public",
        };
    }

    public static bool IsPrimitiveType(ITypeSymbol? parameterType) => parameterType.IsPrimitiveType();

    public static bool IsCollectionType(ITypeSymbol? parameterType, out ITypeSymbol? elementType)
        => IsCollectionType(parameterType, out elementType, out _, false);
    public static bool IsCollectionType(ITypeSymbol? parameterType, out ITypeSymbol? elementType,
        out string castType)
        => IsCollectionType(parameterType, out elementType, out castType, true);

    private static bool IsCollectionType(ITypeSymbol? parameterType, out ITypeSymbol? elementType,
        out string castType, bool getCastType)
    {
        castType = "";
        if (parameterType.IsArray())
        {
            elementType = parameterType.GetContainingTypeSymbol();
            if (getCastType) castType = elementType.GetTypeDisplayName() + "[]";
            return true;
        }

        if (parameterType.IsList())
        {
            elementType = parameterType.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.List<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.IsImmutableArray())
        {
            elementType = parameterType.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Immutable.ImmutableArray<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.ImplementsIList(out var listTypeSymbol))
        {
            elementType = listTypeSymbol.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.IList<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.ImplementsICollection(out var collectionTypeSymbol))
        {
            elementType = collectionTypeSymbol.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.ICollection<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.ImplementsIReadOnlyList(out var readonlyListTypeSymbol))
        {
            elementType = readonlyListTypeSymbol.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.IReadOnlyList<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.ImplementsIReadOnlyCollection(out var readonlyCollectionTypeSymbol))
        {
            elementType = readonlyCollectionTypeSymbol.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.IReadOnlyCollection<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }

        if (parameterType.ImplementsIEnumerable(out var enumerableTypeSymbol))
        {
            elementType = enumerableTypeSymbol.GetContainingTypeSymbol();
            if (getCastType) castType = "global::System.Collections.Generic.IEnumerable<" + elementType.GetTypeDisplayName() + ">";
            return true;
        }
        elementType = null;
        return false;
    }

    [DebuggerDisplay("Order: {Order}; Name: {Name}")]
    public readonly struct MethodParameter
    {
        /// <summary>
        /// Order of parameter in method.
        /// Will be 1 for member1 in method(member0, member1, ...)
        /// </summary>
        public int Order { get; }
        /// <summary>
        /// Type of method parameter
        /// </summary>
        public ITypeSymbol Type { get; }
        /// <summary>
        /// Name of method parameter
        /// </summary>
        public string Name { get; }

        public MethodParameter(int order, ITypeSymbol type, string name)
        {
            Order = order;
            Type = type;
            Name = name;
        }
    }

    [Flags]
    public enum ElementMemberKind
    {
        None = 0,
        RowCount = 1 << 0,
        EstimatedRowCount = 1 << 1,
    }
    public readonly struct ElementMember
    {
        private readonly AttributeData? _dbValue;
        public string DbName => TryGetAttributeValue(_dbValue, "Name", out string? name)
            && !string.IsNullOrWhiteSpace(name) ? name!.Trim() : CodeName;
        public string CodeName => Member.Name;
        public ISymbol Member { get; }
        public ITypeSymbol CodeType => Member switch
        {
            IPropertySymbol prop => prop.Type,
            _ => ((IFieldSymbol)Member).Type,
        };

        public ParameterDirection Direction => TryGetAttributeValue(_dbValue, nameof(Direction), out int direction)
            ? (ParameterDirection)direction : ParameterDirection.Input;

        public ElementMemberKind Kind { get; }

        public bool IsRowCount => (Kind & ElementMemberKind.RowCount) != 0;
        public bool IsEstimatedRowCount => (Kind & ElementMemberKind.EstimatedRowCount) != 0;
        public bool HasDbValueAttribute => _dbValue is not null;

        public T? TryGetValue<T>(string memberName) where T : struct
            => TryGetAttributeValue(_dbValue, memberName, out T value) ? value : null;

        public DbType? GetDbType(out string? readerMethod)
        {
            var dbType = IdentifyDbType(CodeType, out readerMethod);
            if (TryGetAttributeValue(_dbValue, "DbType", out int explicitType, out bool isNull))
            {
                var preferredType = isNull ? (DbType?)null : (DbType)explicitType;
                if (preferredType != dbType)
                {   // only preserve the reader method if this matches
                    readerMethod = null;
                }
            }
            return dbType;
        }

        public bool IsGettable { get; }
        public bool IsSettable { get; }
        public bool IsInitOnly { get; }

        /// <summary>
        /// Order of member in constructor parameter list (starts from 0).
        /// </summary>
        public int? ConstructorParameterOrder { get; }
        /// <summary>
        /// Order of member in factory method parameter list (starts from 0).
        /// </summary>
        public int? FactoryMethodParameterOrder { get; }

        public ElementMember(ISymbol member, AttributeData? dbValue, ElementMemberKind kind)
        {
            Member = member;
            _dbValue = dbValue;
            Kind = kind;
        }

        public ElementMember(
            ISymbol member,
            AttributeData? dbValue,
            ElementMemberKind kind,
            bool isGettable,
            bool isSettable,
            bool isInitOnly,
            int? constructorParameterOrder,
            int? factoryMethodParameterOrder)
        {
            Member = member;
            _dbValue = dbValue;
            Kind = kind;

            IsGettable = isGettable;
            IsSettable = isSettable;
            IsInitOnly = isInitOnly;
            ConstructorParameterOrder = constructorParameterOrder;
            FactoryMethodParameterOrder = factoryMethodParameterOrder;
        }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Member);

        public override string ToString() => Member?.Name ?? "";
        public override bool Equals(object obj) => obj is ElementMember other
            && SymbolEqualityComparer.Default.Equals(Member, other.Member);

        public Location? GetLocation() => Member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.GetLocation();
    }

    public static bool TryGetSingleCompatibleDapperAotFactoryMethod(
        ITypeSymbol? typeSymbol,
        out IMethodSymbol? factoryMethod,
        out Diagnostic? errorDiagnostic)
    {
        errorDiagnostic = null;
        factoryMethod = null!;
        
        var (standardFactories, dapperAotFactories) = ChooseDapperAotCompatibleFactoryMethods(typeSymbol);
        if (standardFactories.Count == 0 && dapperAotFactories.Count == 0)
        {
            errorDiagnostic = null;
            factoryMethod = null!;
            return false;
        }
        
        // if multiple factory methods remain, and multiple are marked [DapperAot]/[DapperAot(true)],
        // a generator error is emitted and no constructor is selected
        if (dapperAotFactories.Count > 1)
        {
            // attaching diagnostic to first location of first ctor
            var loc = dapperAotFactories.First().Locations.First();

            errorDiagnostic = Diagnostic.Create(Diagnostics.TooManyDapperAotEnabledFactoryMethods, loc, typeSymbol!.ToDisplayString());
            factoryMethod = null!;
            return false;
        }

        if (dapperAotFactories.Count == 1)
        {
            errorDiagnostic = null;
            factoryMethod = dapperAotFactories.First();
            return true;
        }

        if (standardFactories.Count == 1)
        {
            errorDiagnostic = null;
            factoryMethod = standardFactories.First();
            return true;
        }
        
        // we cant choose anything
        errorDiagnostic = null;
        factoryMethod = null!;
        return false;
    }
    
    /// <summary>
    /// Chooses a single constructor of type which to use for type's instances creation.
    /// </summary>
    /// <param name="typeSymbol">symbol for type to analyze</param>
    /// <param name="constructor">the method symbol for selected constructor</param>
    /// <param name="errorDiagnostic">if constructor selection was invalid, contains a diagnostic with error to emit to generation context</param>
    /// <returns></returns>
    public static bool TryGetSingleCompatibleDapperAotConstructor(
        ITypeSymbol? typeSymbol,
        out IMethodSymbol? constructor,
        out Diagnostic? errorDiagnostic)
    {
        var (standardCtors, dapperAotEnabledCtors) = ChooseDapperAotCompatibleConstructors(typeSymbol);
        if (standardCtors.Count == 0 && dapperAotEnabledCtors.Count == 0)
        {
            errorDiagnostic = null;
            constructor = null!;
            return false;
        }

        // if multiple constructors remain, and multiple are marked [DapperAot]/[DapperAot(true)], a generator error is emitted and no constructor is selected
        if (dapperAotEnabledCtors.Count > 1)
        {
            // attaching diagnostic to first location of first ctor
            var loc = dapperAotEnabledCtors.First().Locations.First();

            errorDiagnostic = Diagnostic.Create(Diagnostics.TooManyDapperAotEnabledConstructors, loc, typeSymbol!.ToDisplayString());
            constructor = null!;
            return false;
        }

        if (dapperAotEnabledCtors.Count == 1)
        {
            errorDiagnostic = null;
            constructor = dapperAotEnabledCtors.First();
            return true;
        }

        if (standardCtors.Count == 1)
        {
            errorDiagnostic = null;
            constructor = standardCtors.First();
            return true;
        }

        // we cant choose a constructor, so we simply dont choose any
        errorDiagnostic = null;
        constructor = null!;
        return false;
    }
    
    private static (IReadOnlyCollection<IMethodSymbol> standardFactories, IReadOnlyCollection<IMethodSymbol> dapperAotFactories) ChooseDapperAotCompatibleFactoryMethods(ITypeSymbol? typeSymbol)
    {
        bool FilterFactoryMethods(IMethodSymbol methodSymbol) 
            => methodSymbol is { IsStatic: true, DeclaredAccessibility: Accessibility.Public } 
               && SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, typeSymbol);

        var methodSymbols = typeSymbol.GetMethods(filter: FilterFactoryMethods)?.ToImmutableArray();
        if (methodSymbols?.Any() == false)
        {
            return (standardFactories: Array.Empty<IMethodSymbol>(), dapperAotFactories: Array.Empty<IMethodSymbol>());
        }
        
        var standardFactories = new List<IMethodSymbol>();
        var dapperAotFactories = new List<IMethodSymbol>();
        foreach (var methodSymbol in methodSymbols!)
        {
            // not taking into an account parameterless methods
            if (methodSymbol.Parameters.Length == 0) continue;
            
            var hasDapperAotEnabled = HasDapperAotEnabledAttribute(methodSymbol);
            
            if (hasDapperAotEnabled) dapperAotFactories.Add(methodSymbol);
            else standardFactories.Add(methodSymbol);
        }

        return (standardFactories, dapperAotFactories);
    }

    /// <summary>
    /// Builds a collection of type constructors, which are NOT:
    /// a) parameterless
    /// b) marked with [DapperAot(false)]
    /// </summary>
    private static (IReadOnlyCollection<IMethodSymbol> standardConstructors, IReadOnlyCollection<IMethodSymbol> dapperAotEnabledConstructors) ChooseDapperAotCompatibleConstructors(ITypeSymbol? typeSymbol)
    {
        if (!typeSymbol.TryGetConstructors(out var constructors))
        {
            return (standardConstructors: Array.Empty<IMethodSymbol>(), dapperAotEnabledConstructors: Array.Empty<IMethodSymbol>());
        }
        
        // special case
        if (typeSymbol!.IsRecord && constructors?.Length == 2)
        {
            // in case of record syntax with primary constructor like:
            // `public record MyRecord(int Id, string Name);`
            // we need to pick the first constructor, which is the primary one. The second one would contain single parameter of type itself.
            // So checking second constructor suits this rule and picking the first one.

            if (constructors.Value[1].Parameters.Length == 1 && constructors.Value[1].Parameters.First().Type.ToDisplayString() == typeSymbol.ToDisplayString())
            {
                return (standardConstructors: new[] { constructors.Value.First() }, dapperAotEnabledConstructors: Array.Empty<IMethodSymbol>());
            }
        }

        var standardCtors = new List<IMethodSymbol>();
        var dapperAotEnabledCtors = new List<IMethodSymbol>();

        foreach (var constructorMethodSymbol in constructors!)
        {
            // not taking into an account parameterless constructors
            if (constructorMethodSymbol.Parameters.Length == 0) continue;
            
            var hasDapperAotEnabled = HasDapperAotEnabledAttribute(constructorMethodSymbol);
            
            if (hasDapperAotEnabled) dapperAotEnabledCtors.Add(constructorMethodSymbol);
            else standardCtors.Add(constructorMethodSymbol);
        }

        return (standardCtors, dapperAotEnabledCtors);
    }

    /// <summary>
    /// Yields the type's members.
    /// </summary>
    /// <param name="elementType">type, which elements to parse</param>
    /// <param name="dapperAotConstructor"> If is passed, will be used to associate element member with the constructor parameter by name (case-insensitive).</param>
    /// <param name="dapperAotFactoryMethod"> If is passed, will be used to associate element member with the factoryMethod parameter by name (case-insensitive).</param>
    public static IEnumerable<ElementMember> GetMembers(
        ITypeSymbol? elementType,
        IMethodSymbol? dapperAotConstructor = null,
        IMethodSymbol? dapperAotFactoryMethod = null)
    {
        if (elementType is null)
        {
            yield break;
        }
        if (elementType is INamedTypeSymbol named && named.IsTupleType)
        {
            foreach (var field in named.TupleElements)
            {
                yield return new(field, null, ElementMemberKind.None);
            }
        }
        else
        {
            var constructorParameters = (dapperAotConstructor is not null) ? ParseMethodParameters(dapperAotConstructor) : null;
            var factoryMethodParameters = (dapperAotFactoryMethod is not null) ? ParseMethodParameters(dapperAotFactoryMethod) : null;
            
            foreach (var member in elementType.GetMembers())
            {
                // instance only, must be able to access by name
                if (member.IsStatic || !member.CanBeReferencedByName) continue;

                // public or annotated only; not explicitly ignored
                var dbValue = GetDapperAttribute(member, Types.DbValueAttribute);
                var kind = ElementMemberKind.None;
                if (GetDapperAttribute(member, Types.RowCountAttribute) is not null)
                {
                    kind |= ElementMemberKind.RowCount;
                }
                if (GetDapperAttribute(member, Types.EstimatedRowCountAttribute) is not null)
                {
                    kind |= ElementMemberKind.EstimatedRowCount;
                }

                if (dbValue is null && member.DeclaredAccessibility != Accessibility.Public && kind == ElementMemberKind.None) continue;
                if (TryGetAttributeValue(dbValue, "Ignore", out bool ignore) && ignore)
                {
                    continue;
                }

                // field or property (not indexer)
                switch (member)
                {
                    case IPropertySymbol { IsIndexer: false }:
                    case IFieldSymbol:
                        break;
                    default:
                        continue;
                }

                int? constructorParameterOrder = constructorParameters?.TryGetValue(member.Name, out var constructorParameter) == true
                    ? constructorParameter.Order
                    : null;
                int? factoryMethodParameterOrder = factoryMethodParameters?.TryGetValue(member.Name, out var factoryMethodParameter) == true
                    ? factoryMethodParameter.Order
                    : null;

                var isGettable = CodeWriter.IsGettableInstanceMember(member, out _);
                var isSettable = CodeWriter.IsSettableInstanceMember(member, out _);
                var isInitOnly = CodeWriter.IsInitOnlyInstanceMember(member, out _);

                // all good, then!
                yield return new(member, dbValue, kind, isGettable, isSettable, isInitOnly, constructorParameterOrder, factoryMethodParameterOrder);
            }
        }

        IReadOnlyDictionary<string, MethodParameter> ParseMethodParameters(IMethodSymbol constructorSymbol)
        {
            var parameters = new Dictionary<string, MethodParameter>(StringComparer.InvariantCultureIgnoreCase);
            int order = 0;
            foreach (var parameter in constructorSymbol.Parameters)
            {
                parameters.Add(parameter.Name, new MethodParameter(order: order++, type: parameter.Type, name: parameter.Name));
            }
            return parameters;
        }
    }

    private static bool TryGetAttributeValue<T>(AttributeData? attrib, string name, out T? value)
        => TryGetAttributeValue<T>(attrib, name, out value, out _);

    private static bool TryGetAttributeValue<T>(AttributeData? attrib, string name, out T? value, out bool isNull)
    {
        if (attrib is not null)
        {
            foreach (var member in attrib.NamedArguments)
            {
                if (member.Key == name)
                {
                    value = Parse(member.Value, out isNull);
                    return true;
                }
            }
            var ctor = attrib.AttributeConstructor;
            if (ctor is not null)
            {
                int index = 0;
                foreach (var p in ctor.Parameters)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Equals(p.Name == name))
                    {
                        value = Parse(attrib.ConstructorArguments[index], out isNull);
                        return true;
                    }
                    index++;
                }
            }
        }
        value = default;
        isNull = false;
        return false;

        static T? Parse(TypedConstant value, out bool isNull)
        {
            if (isNull = value.IsNull)
            {
                return default;
            }
            if (value.Value is T typed)
            {
                return typed;
            }
            return default;
        }
    }

    //public static ITypeSymbol MakeNullable(ITypeSymbol type)
    //{
    //    if (type is null) return null!; // GIGO
    //    if (type.IsAsync() && type is INamedTypeSymbol named)
    //    {
    //        if (named.TypeArgumentNullableAnnotations.Length == 1 && named.TypeArgumentNullableAnnotations[0] != NullableAnnotation.Annotated)
    //        {
    //            return named.ConstructedFrom.Construct(named.TypeArguments, SingleAnnotated);
    //        }
    //    }
    //    else if (type.NullableAnnotation != NullableAnnotation.Annotated)
    //    {
    //        return type.WithNullableAnnotation(NullableAnnotation.Annotated);
    //    }
    //    return type;
    //}
    private static readonly ImmutableArray<NullableAnnotation> SingleAnnotated = new[] { NullableAnnotation.Annotated }.ToImmutableArray();
    public static ITypeSymbol MakeNonNullable(ITypeSymbol type)
    {
        // think: type = Nullable.GetUnderlyingType(type) ?? type
        if (type.IsValueType && type is INamedTypeSymbol { Arity: 1, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } named)
        {
            return named.TypeArguments[0];
        }
        return type.NullableAnnotation == NullableAnnotation.None
            ? type : type.WithNullableAnnotation(NullableAnnotation.None);
    }

    public static DbType? IdentifyDbType(ITypeSymbol type, out string? readerMethod)
    {
        type = MakeNonNullable(type);
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                readerMethod = nameof(DbDataReader.GetBoolean);
                return DbType.Boolean;
            case SpecialType.System_String:
                readerMethod = nameof(DbDataReader.GetString);
                return DbType.String;
            case SpecialType.System_Single:
                readerMethod = nameof(DbDataReader.GetFloat);
                return DbType.Single;
            case SpecialType.System_Double:
                readerMethod = nameof(DbDataReader.GetDouble);
                return DbType.Double;
            case SpecialType.System_Decimal:
                readerMethod = nameof(DbDataReader.GetDecimal);
                return DbType.Decimal;
            case SpecialType.System_DateTime:
                readerMethod = nameof(DbDataReader.GetDateTime);
                return DbType.DateTime;
            case SpecialType.System_Int16:
                readerMethod = nameof(DbDataReader.GetInt16);
                return DbType.Int16;
            case SpecialType.System_Int32:
                readerMethod = nameof(DbDataReader.GetInt32);
                return DbType.Int32;
            case SpecialType.System_Int64:
                readerMethod = nameof(DbDataReader.GetInt64);
                return DbType.Int64;
            case SpecialType.System_UInt16:
                readerMethod = null;
                return DbType.UInt16;
            case SpecialType.System_UInt32:
                readerMethod = null;
                return DbType.UInt32;
            case SpecialType.System_UInt64:
                readerMethod = null;
                return DbType.UInt64;
            case SpecialType.System_Byte:
                readerMethod = nameof(DbDataReader.GetByte);
                return DbType.Byte;
            case SpecialType.System_SByte:
                readerMethod = null;
                return DbType.SByte;
        }

        if (type.Name == nameof(Guid) && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            readerMethod = nameof(DbDataReader.GetGuid);
            return DbType.Guid;
        }
        if (type.Name == nameof(DateTimeOffset) && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            readerMethod = null;
            return DbType.DateTimeOffset;
        }
        readerMethod = null;
        return null;
    }
}

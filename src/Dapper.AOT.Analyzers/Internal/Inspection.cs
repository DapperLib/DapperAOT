using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
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
        => InvolvesTupleTypeDepthChecked(type, out hasNames, 0);

    private static bool InvolvesTupleTypeDepthChecked(ITypeSymbol? type, out bool hasNames, int depth)
    {
        if (depth >= 20) ThrowPossibleRecursion();

        // avoid SOE
        static void ThrowPossibleRecursion() => throw new InvalidOperationException("Recursion depth exceeded checking for tuple-type");

        while (type is not null) // dive for inheritance
        {
            if (type is IErrorTypeSymbol) break; // nope!

            var named = type as INamedTypeSymbol;
            if (type.IsTupleType)
            {
                hasNames = false;
                if (named is not null)
                {
                    int i = 1;
                    foreach (var field in named.TupleElements)
                    {
                        if (!IsDefaultFieldName(field.Name, i))
                        {
                            hasNames = true;
                            break;
                        }
                        i++;
                    }
                    return true;
                }

                static bool IsDefaultFieldName(string name, int index)
                {
                    if (string.IsNullOrWhiteSpace(name)) return true;
                    return name.StartsWith("Item") && name == $"Item{index}";
                }
            }
            if (type is IArrayTypeSymbol array)
            {
                return InvolvesTupleTypeDepthChecked(array.ElementType, out hasNames, depth + 1);
            }

            if (named is { IsGenericType: true, IsUnboundGenericType: false })
            {
                foreach (var arg in named.TypeArguments)
                {
                    if (InvolvesTupleTypeDepthChecked(arg, out hasNames, depth + 1)) return true;
                }
            }

            type = type.BaseType;
        }
        return hasNames = false;
    }
    public static AttributeData? GetClosestDapperAttribute(in ParseState ctx, IOperation op, string attributeName)
        => GetClosestDapperAttribute(ctx, op, attributeName, out _);
    public static AttributeData? GetClosestDapperAttribute(in ParseState ctx, IOperation op, string attributeName, out Location? location)
    {
        var symbol = GetSymbol(ctx, op);
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

    public static bool IsBasicDapperType(ITypeSymbol? type, string name, TypeKind kind = TypeKind.Class)
        => type is INamedTypeSymbol
        {
            Arity: 0, ContainingType: null,
            ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true }
        } named && named.TypeKind == kind && named.Name == name;

    public static bool IsNestedSqlMapperType(ITypeSymbol? type, string name, TypeKind kind = TypeKind.Class)
    => type is INamedTypeSymbol
    {
        Arity: 0, ContainingType:
        {
            Name: Types.SqlMapper, TypeKind: TypeKind.Class, IsStatic: true, Arity: 0,
            ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true },
        },
    } named && named.TypeKind == kind && named.Name == name;

    public static ISymbol? GetSymbol(in ParseState ctx, IOperation operation)
    {
        var method = GetContainingMethodSyntax(operation);
        return method is null ? null : ctx.SemanticModel.GetDeclaredSymbol(method, ctx.CancellationToken);
        static SyntaxNode? GetContainingMethodSyntax(IOperation op)
        {
            var syntax = op.Syntax;
            while (syntax is not null)
            {
                if (syntax.IsMethodDeclaration())
                {
                    return syntax;
                }
                if (syntax.IsGlobalStatement(out var entryPoint))
                {
                    return entryPoint;
                }
                syntax = syntax.Parent;
            }
            return null;
        }
    }

    // support the fact that [DapperAot(bool)] can enable/disable generation at any level
    // including method, type, module and assembly; first attribute found (walking up the tree): wins
    public static bool IsEnabled(in ParseState ctx, IOperation op, string attributeName, out bool exists)
    {
        var attrib = GetClosestDapperAttribute(ctx, op, attributeName);
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

    public static AttributeData? GetAttribute(ISymbol? symbol, string attributeName)
    {
        if (symbol is not null)
        {
            foreach (var attrib in symbol.GetAttributes())
            {
                if (attrib.AttributeClass!.Name == attributeName)
                {
                    return attrib;
                }
            }
        }

        return null;
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

    internal static bool IsDynamicParameters(ITypeSymbol? type, out bool needsConstruction)
    {
        needsConstruction = false;
        if (type is null || type.SpecialType != SpecialType.None) return false;
        if (IsBasicDapperType(type, Types.DynamicParameters)
            || IsNestedSqlMapperType(type, Types.IDynamicParameters, TypeKind.Interface)) return true;
        foreach (var i in type.AllInterfaces)
        {
            if (IsNestedSqlMapperType(i, Types.IDynamicParameters, TypeKind.Interface)) return true;
        }

        // Dapper also treats anything IEnumerable<string,object[?]> as dynamic parameters
        if (type.ImplementsIEnumerable(out var found) && found is INamedTypeSymbol { Arity: 1 } iet
            && iet.TypeArguments[0] is INamedTypeSymbol
            {
                Name: "KeyValuePair", Arity: 2, ContainingType: null,
                ContainingNamespace:
                {
                    Name: "Generic",
                    ContainingNamespace:
                    {
                        Name: "Collections",
                        ContainingNamespace:
                        {
                            Name: "System",
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    }
                }
            } kvp
            && kvp.TypeArguments[0].SpecialType == SpecialType.System_String
            && kvp.TypeArguments[1].SpecialType == SpecialType.System_Object)
        {
            needsConstruction = true;
            return true;
        }
        return false;
    }

    public static bool IsPublicOrAssemblyLocal(ISymbol? symbol, in ParseState ctx, out ISymbol? failingSymbol)
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
        if (parameterType is null || parameterType.SpecialType == SpecialType.System_String)
        {
            elementType = null;
            return false;
        }

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
    readonly struct MethodParameter
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
        RowCountHint = 1 << 1,
        Cancellation = 1 << 2,
    }

    [Flags]
    internal enum DapperSpecialType
    {
        None = 0,
        DbString = 1 << 0,
    }

    internal static bool IsCancellationToken(ITypeSymbol? type)
           => type is INamedTypeSymbol
           {
               Name: nameof(CancellationToken),
               Arity: 0,
               TypeKind: TypeKind.Struct,
               ContainingType: null,
               ContainingNamespace:
               {
                   Name: "Threading",
                   ContainingNamespace:
                   {
                       Name: "System",
                       ContainingNamespace.IsGlobalNamespace: true
                   }
               }
           };

    public readonly struct ElementMember
    {
        public Location[]? AsAdditionalLocations(string? attributeName = null, bool allowNonDapperLocations = false)
        {
            var loc = GetLocation(attributeName, allowNonDapperLocations);
            return loc is null ? null : [loc];
        }

        private readonly AttributeData? _dbValue;
        public readonly ColumnAttributeData ColumnAttributeData { get; } = ColumnAttributeData.Default;
        public string DbName
        {
            get
            {
                if (TryGetAttributeValue(_dbValue, "Name", out string? name) && !string.IsNullOrWhiteSpace(name))
                {
                    // priority 1: [DbValue] attribute
                    return name!.Trim();
                }

                if (ColumnAttributeData.IsCorrectUsage)
                {
                    // priority 2: [Column] attribute
                    return ColumnAttributeData.Name!;
                }

                return CodeName;
            }
        }
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

        public SymbolKind SymbolKind => Member.Kind;

        public bool IsRowCount => (Kind & ElementMemberKind.RowCount) != 0;
        public bool IsRowCountHint => (Kind & ElementMemberKind.RowCountHint) != 0;
        public bool IsCancellation => (Kind & ElementMemberKind.Cancellation) != 0;
        public bool HasDbValueAttribute => _dbValue is not null;

        public DapperSpecialType DapperSpecialType 
        { 
            get 
            {
                if (CodeType is {
                    Name: "DbString",
                    TypeKind: TypeKind.Class,
                    ContainingNamespace:
                    {
                        Name: "Dapper",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }) return DapperSpecialType.DbString;
                
                return DapperSpecialType.None;
            } 
        }

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
                    dbType = preferredType;
                }
            }
            return dbType;
        }

        private readonly ElementMemberFlags _flags;
        public ElementMemberFlags Flags => _flags;
        public bool IsGettable => (_flags & ElementMemberFlags.IsGettable) != 0;
        public bool IsSettable => (_flags & ElementMemberFlags.IsSettable) != 0;
        public bool IsInitOnly => (_flags & ElementMemberFlags.IsInitOnly) != 0;
        public bool IsRequired => (_flags & ElementMemberFlags.IsRequired) != 0;
        public bool IsExpandable => (_flags & ElementMemberFlags.IsExpandable) != 0;

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
            if (IsCancellationToken(CodeType)) Kind |= ElementMemberKind.Cancellation;
        }

        [Flags]
        public enum ElementMemberFlags
        {
            None = 0,
            IsGettable = 1 << 0,
            IsSettable = 1 << 1,
            IsInitOnly = 1 << 2,
            IsExpandable = 1 << 3,
            IsRequired = 1 << 4,
        }

        public ElementMember(
            ISymbol member,
            AttributeData? dbValue,
            ColumnAttributeData columnAttributeData,
            ElementMemberKind kind,
            ElementMemberFlags flags,
            int? constructorParameterOrder,
            int? factoryMethodParameterOrder)
        {
            _dbValue = dbValue;
            _flags = flags;
            ColumnAttributeData = columnAttributeData;

            Member = member;
            Kind = kind;

            ConstructorParameterOrder = constructorParameterOrder;
            FactoryMethodParameterOrder = factoryMethodParameterOrder;
            if (IsCancellationToken(CodeType)) Kind |= ElementMemberKind.Cancellation;
        }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Member);

        public override string ToString() => Member?.Name ?? "";
        public override bool Equals(object obj) => obj is ElementMember other
            && SymbolEqualityComparer.Default.Equals(Member, other.Member);

        public Location? GetLocation()
        {
            // `ISymbol.Locations` gives the best location of member
            // (i.e. for property it will be NAME of an element without modifiers \ getters and setters),
            // but it also returns implicitly defined members -> so we need to double check if that can be used
            if (Member.Locations.Length > 0)
            {
                var sourceLocation = Member.Locations.FirstOrDefault(loc => loc.IsInSource);
                if (sourceLocation is not null) return sourceLocation;
            }

            foreach (var node in Member.DeclaringSyntaxReferences)
            {
                if (node.GetSyntax().GetLocation() is { } loc)
                {
                    return loc;
                }
            }
            return null;
        }
        public Location? GetLocation(string? attributeName = null, bool allowNonDapperLocations = false)
        {
            if (attributeName is null)
            {
                return GetLocation();
            }

            var result = allowNonDapperLocations
                ? GetAttribute(Member, attributeName)?.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
                : GetDapperAttribute(Member, attributeName)?.ApplicationSyntaxReference?.GetSyntax()?.GetLocation();

            return result ?? GetLocation();
        }
    }

    internal struct ColumnAttributeData
    {
        public UseColumnAttributeState UseColumnAttribute { get; }
        public ColumnAttributeState ColumnAttribute { get; }
        public string? Name { get; }

        public ColumnAttributeData(UseColumnAttributeState useColumnAttribute, ColumnAttributeState columnAttribute, string? name)
        {
            UseColumnAttribute = useColumnAttribute;
            ColumnAttribute = columnAttribute;
            Name = name;
        }

        public bool IsCorrectUsage
            => UseColumnAttribute is UseColumnAttributeState.NotSpecified or UseColumnAttributeState.Enabled
            && ColumnAttribute is ColumnAttributeState.Specified && !string.IsNullOrEmpty(Name);

        public static ColumnAttributeData Default => new(UseColumnAttributeState.NotSpecified, ColumnAttributeState.NotSpecified, null);

        public enum UseColumnAttributeState
        {
            NotSpecified,
            Disabled,
            Enabled
        }

        public enum ColumnAttributeState
        {
            NotSpecified,
            Specified
        }
    }

    public enum ConstructorResult
    {
        NoneFound,
        SuccessSingleExplicit,
        SuccessSingleImplicit,
        FailMultipleExplicit,
        FailMultipleImplicit
    }

    public enum FactoryMethodResult
    {
        // note that implicit isn't a thing here
        NoneFound,
        SuccessSingleExplicit,
        FailMultipleExplicit
    }

    /// <summary>
    /// Tries to choose a single factory method for the type.
    /// _Note:_ factory method is a 1) publicly visibly; 2) static method 3) with response type equal to containing type
    /// </summary>
    internal static FactoryMethodResult ChooseFactoryMethod(ITypeSymbol? typeSymbol, out IMethodSymbol? factoryMethod)
    {
        factoryMethod = null;
        if (typeSymbol is null)
        {
            return FactoryMethodResult.NoneFound;
        }

        var staticMethods = typeSymbol
            .GetMethods(method =>
                method.IsStatic &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, typeSymbol) &&
                method.DeclaredAccessibility == Accessibility.Public
            )
            ?.ToArray();
        if (staticMethods?.Length == 0)
        {
            return FactoryMethodResult.NoneFound;
        }

        foreach (var method in staticMethods!)
        {
            if (GetDapperAttribute(method, Types.ExplicitConstructorAttribute) is not null)
            {
                if (factoryMethod is not null)
                {
                    // note pointing to first found method for diagnostic
                    return FactoryMethodResult.FailMultipleExplicit;
                }
                factoryMethod = method;
            }
        }

        return factoryMethod is null ? FactoryMethodResult.NoneFound : FactoryMethodResult.SuccessSingleExplicit;
    }

    /// <summary>
    /// Builds a collection of type constructors, which are NOT:
    /// a) parameterless
    /// b) marked with [DapperAot(false)]
    /// </summary>
    internal static ConstructorResult ChooseConstructor(ITypeSymbol? typeSymbol, out IMethodSymbol? constructor)
    {
        constructor = null;
        if (typeSymbol is not INamedTypeSymbol named)
        {
            return ConstructorResult.NoneFound;
        }

        var ctors = named.Constructors;
        if (ctors.IsDefaultOrEmpty)
        {
            return ConstructorResult.NoneFound;
        }

        // look for [ExplicitConstructor]
        foreach (var ctor in ctors)
        {
            if (GetDapperAttribute(ctor, Types.ExplicitConstructorAttribute) is not null)
            {
                if (constructor is not null)
                {
                    return ConstructorResult.FailMultipleExplicit;
                }
                constructor = ctor;
            }
        }
        if (constructor is not null)
        {
            // we found one [ExplicitConstructor]
            return ConstructorResult.SuccessSingleExplicit;
        }

        // look for remaining constructors
        foreach (var ctor in ctors)
        {
            switch (ctor.DeclaredAccessibility)
            {
                case Accessibility.Private:
                case Accessibility.Protected:
                case Accessibility.Friend:
                case Accessibility.ProtectedAndInternal:
                    continue; // we can't use it, so...
            }
            var args = ctor.Parameters;
            if (args.Length == 0) continue; // default constructor

            // exclude copy constructors (anything that takes the own type) and serialization constructors
            bool exclude = false;
            foreach (var arg in args)
            {
                if (SymbolEqualityComparer.Default.Equals(arg.Type, named))
                {
                    exclude = true;
                    break;
                }
                if (arg.Type is INamedTypeSymbol
                    {
                        Name: "SerializationInfo" or "StreamingContext",
                        ContainingType: null,
                        Arity: 0,
                        ContainingNamespace:
                        {
                            Name: "Serialization",
                            ContainingNamespace:
                            {
                                Name: "Runtime",
                                ContainingNamespace:
                                {
                                    Name: "System",
                                    ContainingNamespace.IsGlobalNamespace: true
                                }
                            }
                        }
                    })
                {
                    exclude = true;
                    break;
                }
            }
            if (exclude)
            {
                // ruled out by signature
                continue;
            }

            if (constructor is not null)
            {
                return ConstructorResult.FailMultipleImplicit;
            }
            constructor = ctor;
        }
        return constructor is null ? ConstructorResult.NoneFound : ConstructorResult.SuccessSingleImplicit;
    }

    /// <summary>
    /// Yields the type's members.
    /// If <param name="constructor"/> is passed, will be used to associate element member with the constructor parameter by name (case-insensitive).
    /// </summary>
    /// <param name="elementType">type, which elements to parse</param>
    /// <param name="constructor">pointer to single constructor available for AOT scenario</param>
    /// <param name="factoryMethod">pointer to single factoryMethod available for AOT scenario</param>
    internal static ImmutableArray<ElementMember> GetMembers(bool forParameters, ITypeSymbol? elementType, IMethodSymbol? constructor, IMethodSymbol? factoryMethod)
    {
        if (elementType is null)
        {
            return ImmutableArray<ElementMember>.Empty;
        }
        if (elementType is INamedTypeSymbol named && named.IsTupleType)
        {
            return ImmutableArray.CreateRange(named.TupleElements, field => new ElementMember(field, null, ElementMemberKind.None));
        }
        else
        {
            var tier = elementType;

            var builder = ImmutableArray.CreateBuilder<ElementMember>();
            while (tier is not null or IErrorTypeSymbol)
            {
                var elMembers = tier.GetMembers(); // walk hierarchy model

                var constructorParameters = (constructor is not null) ? ParseMethodParameters(constructor) : null;
                var factoryMethodParameters = (factoryMethod is not null) ? ParseMethodParameters(factoryMethod) : null;
                foreach (var member in elMembers)
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
                    if (GetDapperAttribute(member, Types.RowCountHintAttribute) is not null)
                    {
                        kind |= ElementMemberKind.RowCountHint;
                    }

                    if (dbValue is null && member.DeclaredAccessibility != Accessibility.Public && kind == ElementMemberKind.None) continue;
                    if (TryGetAttributeValue(dbValue, "Ignore", out bool ignore) && ignore)
                    {
                        continue;
                    }

                    // field or property (not indexer)
                    ITypeSymbol memberType;
                    switch (member)
                    {
                        case IPropertySymbol { IsIndexer: false } prop:
                            memberType = prop.Type;
                            break;
                        case IFieldSymbol field:
                            memberType = field.Type;
                            break;
                        default:
                            continue;
                    }
                    if (memberType is null) continue;

                    int? constructorParameterOrder = constructorParameters?.TryGetValue(member.Name, out var constructorParameter) == true
                        ? constructorParameter.Order
                        : null;

                    int? factoryMethodParamOrder = factoryMethodParameters?.TryGetValue(member.Name, out var factoryMethodParam) == true
                        ? factoryMethodParam.Order
                        : null;

                    ElementMember.ElementMemberFlags flags = ElementMember.ElementMemberFlags.None;
                    if (CodeWriter.IsGettableInstanceMember(member, out _)) flags |= ElementMember.ElementMemberFlags.IsGettable;
                    if (CodeWriter.IsSettableInstanceMember(member, out _)) flags |= ElementMember.ElementMemberFlags.IsSettable;
                    if (CodeWriter.IsInitOnlyInstanceMember(member, out _)) flags |= ElementMember.ElementMemberFlags.IsInitOnly;
                    if (CodeWriter.IsRequired(member)) flags |= ElementMember.ElementMemberFlags.IsRequired;

                    if (forParameters)
                    {
                        // needs to be readable
                        if ((flags & ElementMember.ElementMemberFlags.IsGettable) == 0) continue;
                    }
                    else
                    {
                        // needs to be writable
                        if (constructorParameterOrder is null && factoryMethodParamOrder is null &&
                            (flags & (ElementMember.ElementMemberFlags.IsSettable | ElementMember.ElementMemberFlags.IsInitOnly)) == 0) continue;
                    }

                    // see Dapper's TryStringSplit logic
                    if (IsCollectionType(memberType, out var innerType) && innerType is not null)
                    {
                        flags |= ElementMember.ElementMemberFlags.IsExpandable;
                    }

                    var columnAttributeData = ParseColumnAttributeData(member);

                    // all good, then!
                    builder.Add(new(member, dbValue, columnAttributeData, kind, flags, constructorParameterOrder, factoryMethodParamOrder));
                }

                tier = tier.BaseType;
            }

            return builder.ToImmutable();
        }

        static ColumnAttributeData ParseColumnAttributeData(ISymbol? member)
        {
            if (member is null) return ColumnAttributeData.Default;
            var useColumnAttributeState = ParseUseColumnAttributeState();
            var (columnAttributeState, columnName) = ParseColumnAttributeState();

            return new ColumnAttributeData(useColumnAttributeState, columnAttributeState, columnName);

            (ColumnAttributeData.ColumnAttributeState state, string? columnName) ParseColumnAttributeState()
            {
                var columnAttribute = GetAttribute(member, Types.ColumnAttribute);
                if (columnAttribute is null) return (ColumnAttributeData.ColumnAttributeState.NotSpecified, null);

                if (TryGetAttributeValue(columnAttribute, "name", out string? name) && !string.IsNullOrWhiteSpace(name))
                {
                    return (ColumnAttributeData.ColumnAttributeState.Specified, name);
                }
                else
                {
                    // [Column] is specified, but value is null. Can happen for ctor overload: 
                    // https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute.-ctor?view=net-7.0#system-componentmodel-dataannotations-schema-columnattribute-ctor
                    return (ColumnAttributeData.ColumnAttributeState.Specified, null);
                }
            }
            ColumnAttributeData.UseColumnAttributeState ParseUseColumnAttributeState()
            {
                var useColumnAttribute = GetDapperAttribute(member, Types.UseColumnAttributeAttribute);
                if (useColumnAttribute is null) return ColumnAttributeData.UseColumnAttributeState.NotSpecified;

                if (TryGetAttributeValue(useColumnAttribute, "enable", out bool useColumnAttributeState))
                {
                    return useColumnAttributeState ? ColumnAttributeData.UseColumnAttributeState.Enabled : ColumnAttributeData.UseColumnAttributeState.Disabled;
                }
                else
                {
                    // default
                    return ColumnAttributeData.UseColumnAttributeState.Enabled;
                }
            }
        }

        static IReadOnlyDictionary<string, MethodParameter> ParseMethodParameters(IMethodSymbol constructorSymbol)
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
                    if (StringComparer.InvariantCultureIgnoreCase.Equals(p.Name, name))
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

    /// <summary>
    /// There are special types we are allowing user to query,
    /// which are not handled in the general checks
    /// </summary>
    public static bool IsSpecialCaseAllowedResultType(ITypeSymbol? type)
    {
        // byte[] or sbyte[] are basically blobs
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte or SpecialType.System_SByte })
        {
            return true;
        }

        return false;
    }

    public static DbType? IdentifyDbType(ITypeSymbol? type, out string? readerMethod)
    {
        if (type is null)
        {
            readerMethod = null;
            return null;
        }
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
        if (type.Name == "DbString" && type.ContainingNamespace is { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true })
        {
            readerMethod = null;
            return DbType.String;
        }
        readerMethod = null;
        return null;
    }

    public static bool IsDapperMethod(this IInvocationOperation operation, out OperationFlags flags)
    {
        flags = OperationFlags.None;
        var method = operation?.TargetMethod;
        if (method is null || !method.IsExtensionMethod)
        {
            return false;
        }
        var type = method.ContainingType;
        if (type is not { Name: Types.SqlMapper, ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
        {
            return false;
        }
        if (method.Name.EndsWith("Async"))
        {
            flags |= OperationFlags.Async;
        }
        if (method.IsGenericMethod)
        {
            flags |= OperationFlags.TypedResult;
        }
        switch (method.Name)
        {
            case "Query":
                flags |= OperationFlags.Query;
                if (method.Arity > 1) flags |= OperationFlags.NotAotSupported;
                break;
            case "QueryAsync":
            case "QueryUnbufferedAsync":
                flags |= method.Name.Contains("Unbuffered") ? OperationFlags.Unbuffered : OperationFlags.Buffered;
                goto case "Query";
            case "QueryFirst":
            case "QueryFirstAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne;
                goto case "Query";
            case "QueryFirstOrDefault":
            case "QueryFirstOrDefaultAsync":
                flags |= OperationFlags.SingleRow;
                goto case "Query";
            case "QuerySingle":
            case "QuerySingleAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne;
                goto case "Query";
            case "QuerySingleOrDefault":
            case "QuerySingleOrDefaultAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtMostOne;
                goto case "Query";
            case "QueryMultiple":
            case "QueryMultipleAsync":
                flags |= OperationFlags.Query | OperationFlags.QueryMultiple | OperationFlags.NotAotSupported;
                break;
            case "Execute":
            case "ExecuteAsync":
                flags |= OperationFlags.Execute;
                if (method.Arity != 0) flags |= OperationFlags.NotAotSupported;
                break;
            case "ExecuteScalar":
            case "ExecuteScalarAsync":
                flags |= OperationFlags.Execute | OperationFlags.Scalar;
                if (method.Arity >= 2) flags |= OperationFlags.NotAotSupported;
                break;
            case "GetRowParser":
                flags |= OperationFlags.GetRowParser;
                if (method.Arity != 1) flags |= OperationFlags.NotAotSupported;
                break;
            default:
                flags = OperationFlags.NotAotSupported;
                break;
        }
        return true;
    }
    public static bool HasAny(this OperationFlags value, OperationFlags testFor) => (value & testFor) != 0;
    public static bool HasAll(this OperationFlags value, OperationFlags testFor) => (value & testFor) == testFor;

    public static bool TryGetConstantValue<T>(IOperation op, out T? value)
            => TryGetConstantValueWithSyntax(op, out value, out _, out _);
    
    public static ITypeSymbol? GetResultType(this IInvocationOperation invocation, OperationFlags flags)
    {
        if (flags.HasAny(OperationFlags.TypedResult))
        {
            var typeArgs = invocation.TargetMethod.TypeArguments;
            if (typeArgs.Length == 1)
            {
                return typeArgs[0];
            }
        }
        return null;
    }

    internal static bool CouldBeNullable(ITypeSymbol symbol) => symbol.IsValueType
        ? symbol.NullableAnnotation == NullableAnnotation.Annotated
        : symbol.NullableAnnotation != NullableAnnotation.NotAnnotated;

    public static bool TryGetConstantValueWithSyntax<T>(IOperation val, out T? value, out SyntaxNode? syntax, out StringSyntaxKind? syntaxKind)
    {
        try
        {
            if (val.ConstantValue.HasValue)
            {
                value = (T?)val.ConstantValue.Value;
                syntax = val.Syntax;
                syntaxKind = val.TryDetectOperationStringSyntaxKind();
                return true;
            }
            if (val is IArgumentOperation arg)
            {
                val = arg.Value;
            }
            // work through any implicit/explicit conversion steps
            while (val is IConversionOperation conv)
            {
                val = conv.Operand;
            }

            if (!val.ConstantValue.HasValue)
            {
                var stringSyntaxKind = val.TryDetectOperationStringSyntaxKind();
                switch (stringSyntaxKind)
                {
                    case StringSyntaxKind.ConcatenatedString:
                    case StringSyntaxKind.InterpolatedString:
                    case StringSyntaxKind.FormatString:
                        {
                            value = default!;
                            syntax = null;
                            syntaxKind = stringSyntaxKind;
                            return false;
                        }
                }
            }

            // type-level constants
            if (val is IFieldReferenceOperation field && field.Field.HasConstantValue)
            {
                value = (T?)field.Field.ConstantValue;
                syntax = field.Field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                syntaxKind = val.TryDetectOperationStringSyntaxKind();
                return true;
            }

            // local constants
            if (val is ILocalReferenceOperation local && local.Local.HasConstantValue)
            {
                value = (T?)local.Local.ConstantValue;
                syntax = local.Local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                syntaxKind = val.TryDetectOperationStringSyntaxKind();
                return true;
            }

            // other non-trivial default constant values
            if (val.ConstantValue.HasValue)
            {
                value = (T?)val.ConstantValue.Value;
                syntax = val.Syntax;
                syntaxKind = val.TryDetectOperationStringSyntaxKind();
                return true;
            }

            // other trivial default constant values
            if (val is ILiteralOperation or IDefaultValueOperation)
            {
                // we already ruled out explicit constant above, so: must be default
                value = default;
                syntax = val.Syntax;
                syntaxKind = val.TryDetectOperationStringSyntaxKind();
                return true;
            }
        }
        catch { }
        value = default!;
        syntax = null;
        syntaxKind = StringSyntaxKind.NotRecognized;
        return false;
    }

    internal static bool IsCommand(INamedTypeSymbol type)
    {
        switch (type.TypeKind)
        {
            case TypeKind.Interface:
                return type is
                {
                    Name: nameof(IDbCommand),
                    ContainingType: null,
                    Arity: 0,
                    IsGenericType: false,
                    ContainingNamespace:
                    {
                        Name: "Data",
                        ContainingNamespace:
                        {
                            Name: "System",
                            ContainingNamespace.IsGlobalNamespace: true,
                        }
                    }
                };
            case TypeKind.Class when !type.IsStatic:
                while (type.SpecialType != SpecialType.System_Object)
                {
                    if (type is
                        {
                            Name: nameof(DbCommand),
                            ContainingType: null,
                            Arity: 0,
                            IsGenericType: false,
                            ContainingNamespace:
                            {
                                Name: "Common",
                                ContainingNamespace:
                                {
                                    Name: "Data",
                                    ContainingNamespace:
                                    {
                                        Name: "System",
                                        ContainingNamespace.IsGlobalNamespace: true,
                                    }
                                }
                            }
                        })
                    {
                        return true;
                    }
                    if (type.BaseType is null) break;
                    type = type.BaseType;
                }
                break;
        }
        return false;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "Fine as is; let's not pay the unwrap cost")]
    public static SqlSyntax? IdentifySqlSyntax(in ParseState ctx, IOperation op, out bool caseSensitive)
    {
        caseSensitive = false;

        // get fom [SqlSyntax(...)] hint
        var attrib = GetClosestDapperAttribute(ctx, op, Types.SqlSyntaxAttribute);
        if (attrib is not null && attrib.ConstructorArguments.Length == 1 && attrib.ConstructorArguments[0].Value is int i)
        {
            return (SqlSyntax)i;
        }

        // get from known connection-type
        if (op is IInvocationOperation invoke)
        {
            IOperation? target = invoke.Instance;
            if (target is null && invoke.TargetMethod.IsExtensionMethod && !invoke.Arguments.IsDefaultOrEmpty)
            {
                target = invoke.Arguments[0].Value;
            }
            var targetType = target is IConversionOperation conv ? conv.Operand.Type : target?.Type;
            if (targetType is INamedTypeSymbol { Arity: 0, ContainingType: null } type)
            {
                var ns = type.ContainingNamespace;
                foreach (var candidate in KnownConnectionTypes)
                {
                    var current = ns;
                    if (type.Name == candidate.Connection
                        && AssertAndAscend(ref current, candidate.Namespace0)
                        && AssertAndAscend(ref current, candidate.Namespace1)
                        && AssertAndAscend(ref current, candidate.Namespace2))
                    {
                        return candidate.Syntax;
                    }
                }
            }
        }
        else if (op is ISimpleAssignmentOperation assign && assign.Target is IMemberReferenceOperation member)
        {
            if (member.Member.ContainingType is INamedTypeSymbol { Arity: 0, ContainingType: null } type)
            {
                var ns = type.ContainingNamespace;
                foreach (var candidate in KnownConnectionTypes)
                {
                    var current = ns;
                    if (type.Name == candidate.Command
                        && AssertAndAscend(ref current, candidate.Namespace0)
                        && AssertAndAscend(ref current, candidate.Namespace1)
                        && AssertAndAscend(ref current, candidate.Namespace2))
                    {
                        return candidate.Syntax;
                    }
                }
            }
        }

        return null;

        static bool AssertAndAscend(ref INamespaceSymbol ns, string? expected)
        {
            if (expected is null)
            {
                return ns.IsGlobalNamespace;
            }
            else
            {
                if (ns.Name == expected)
                {
                    ns = ns.ContainingNamespace;
                    return true;
                }
                return false;
            }
        }
    }

    private static readonly ImmutableArray<(string? Namespace2, string? Namespace1, string Namespace0, string Connection, string Command, SqlSyntax Syntax)> KnownConnectionTypes = new[]
    {
            ("System", "Data", "SqlClient", "SqlConnection", "SqlCommand", SqlSyntax.SqlServer),
            ("Microsoft", "Data", "SqlClient", "SqlConnection", "SqlCommand", SqlSyntax.SqlServer),

            (null, null, "Npgsql", "NpgsqlConnection", "NpgsqlCommand", SqlSyntax.PostgreSql),

            ("MySql", "Data", "MySqlClient", "MySqlConnection", "MySqlCommand", SqlSyntax.MySql),

            ("Oracle", "DataAccess", "Client", "OracleConnection", "OracleCommand", SqlSyntax.Oracle),

            ("Microsoft", "Data", "Sqlite", "SqliteConnection", "SqliteCommand", SqlSyntax.SQLite),
        }.ToImmutableArray();
}

enum DapperMethodKind
{
    NotDapper,
    DapperUnsupported,
    DapperSupported,
}

[Flags]
enum OperationFlags
{
    None = 0,
    Query = 1 << 0,
    Execute = 1 << 1,
    Async = 1 << 2,
    TypedResult = 1 << 3,
    HasParameters = 1 << 4,
    Buffered = 1 << 5,
    Unbuffered = 1 << 6,
    SingleRow = 1 << 7,
    Text = 1 << 8,
    StoredProcedure = 1 << 9,
    TableDirect = 1 << 10,
    AtLeastOne = 1 << 11,
    AtMostOne = 1 << 12,
    Scalar = 1 << 13,
    DoNotGenerate = 1 << 14,
    AotNotEnabled = 1 << 15,
    AotDisabled = 1 << 16, // actively disabled
    BindResultsByName = 1 << 17,
    BindTupleParameterByName = 1 << 18,
    CacheCommand = 1 << 19,
    IncludeLocation = 1 << 20, // include -- SomeFile.cs#40 when possible
    KnownParameters = 1 << 21,
    QueryMultiple = 1 << 22,
    GetRowParser = 1 << 23,

    NotAotSupported = 1 << 31,
}

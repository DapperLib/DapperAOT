using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

    // support the fact that [DapperAot(bool)] can enable/disable generation at any level
    // including method, type, module and assembly; first attribute found (walking up the tree): wins
    public static bool IsEnabled(in GeneratorSyntaxContext ctx, IOperation op, string attributeName, out bool exists, CancellationToken cancellationToken)
    {
        var method = GetContainingMethodSyntax(op);
        if (method is not null)
        {
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
            while (symbol is not null)
            {
                if (HasDapperAttribute(symbol, attributeName, out bool enabled))
                {
                    exists = true;
                    return enabled;
                }
                symbol = symbol.ContainingSymbol;
            }
        }

        // disabled globally by default
        return exists = false;

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
        static bool HasDapperAttribute(ISymbol? symbol, string attributeName, out bool enabled)
        {
            if (symbol is not null)
            {
                foreach (var attrib in symbol.GetAttributes())
                {
                    if (attrib.AttributeClass is
                        {
                            ContainingNamespace:
                            {
                                Name: "Dapper",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    && attrib.AttributeClass.Name == attributeName
                    && attrib.ConstructorArguments.Length == 1
                    && attrib.ConstructorArguments[0].Value is bool b)
                    {
                        enabled = b;
                        return true;
                    }
                }
            }

            enabled = default;
            return false;
        }
    }

    public static AttributeData? GetDapperAttribute(ISymbol? symbol, string attributeName)
    {
        if (symbol is not null)
        {
            foreach (var attrib in symbol.GetAttributes())
            {
                if (attrib.AttributeClass is
                    {
                        ContainingNamespace:
                        {
                            Name: "Dapper",
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    }
                && attrib.AttributeClass.Name == attributeName)
                {
                    return attrib;
                }
            }
        }
        return null;
    }

    public static bool IsMissingOrObjectOrDynamic(ITypeSymbol? type) => type is null || type.SpecialType == SpecialType.System_Object || type.TypeKind == TypeKind.Dynamic;

    public static bool IsPublicOrAssemblyLocal(ISymbol symbol, in GeneratorSyntaxContext ctx, out ISymbol? failingSymbol)
        => IsPublicOrAssemblyLocal(symbol, ctx.SemanticModel.Compilation.Assembly, out failingSymbol);

    public static bool IsPublicOrAssemblyLocal(ISymbol symbol, IAssemblySymbol? assembly, out ISymbol? failingSymbol)
    {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
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

        public bool IsRowCount { get; }
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

        public ElementMember(ISymbol member, AttributeData? dbValue, bool isRowCount)
        {
            Member = member;
            _dbValue = dbValue;
            IsRowCount = isRowCount;
        }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(Member);

        public override string ToString() => Member?.Name ?? "";
        public override bool Equals(object obj) => obj is ElementMember other
            && SymbolEqualityComparer.Default.Equals(Member, other.Member);
    }

    public static IEnumerable<ElementMember> GetMembers(ITypeSymbol elementType)
    {
        if (elementType is INamedTypeSymbol named && named.IsTupleType)
        {
            foreach (var field in named.TupleElements)
            {
                yield return new(field, null, false);
            }
        }
        else
        {
            foreach (var member in elementType!.GetMembers())
            {
                // instance only, must be able to access by name
                if (member.IsStatic || !member.CanBeReferencedByName) continue;

                // public or annotated only; not explicitly ignored
                var dbValue = GetDapperAttribute(member, Types.DbValueAttribute);
                var isRowCount = GetDapperAttribute(member, Types.RowCountAttribute) is not null;
                if (dbValue is null && member.DeclaredAccessibility != Accessibility.Public && !isRowCount) continue;
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

                // all good, then!
                yield return new(member, dbValue, isRowCount);
            }
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

    public static ITypeSymbol MakeNonNullable(ITypeSymbol type)
    {
        // think: type = Nullable.GetUnderlyingType(type) ?? type
        if (type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated
            && type is INamedTypeSymbol named && named.Arity == 1)
        {
            type = named.TypeArguments[0];
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
        //if (type is IArrayTypeSymbol array && array.IsSZArray) // SZArray === "vector" (1-dim, 0-based)
        //{
        // byte
        //}

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

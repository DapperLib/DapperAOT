using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using static Dapper.Internal.Inspection;

namespace Dapper.Internal;

internal sealed class MemberMap
{
    public ITypeSymbol DeclaredType { get; }
    public ITypeSymbol ElementType { get; }
    public string? CSharpCastType { get; }

    private readonly MapFlags _flags;

    public IMethodSymbol? Constructor { get; }
    public bool IsUnknownParameters => (_flags & (MapFlags.IsObject | MapFlags.IsDapperDynamic)) != 0;

    public bool IsObject => (_flags & MapFlags.IsObject) != 0;
    public bool IsDapperDynamic => (_flags & MapFlags.IsDapperDynamic) != 0;
    public bool IsCollection => (_flags & MapFlags.IsCollection) != 0;
    public ImmutableArray<ElementMember> Members { get; }

    enum MapFlags
    {
        None = 0,
        IsObject = 1 << 0,
        IsDapperDynamic = 1 << 1,
        IsCollection = 1 << 2,
    }

    public static MemberMap? Create(IOperation? parameters)
        => parameters is null or { Type: null} ? null : new (parameters.Syntax.GetLocation(), parameters.Type, parameters);

    public static MemberMap? Create(ITypeSymbol? type, Location location)
        => type is null ? null : new(location, type, null);

    public IOperation? Parameters { get; }
    public Location Location { get; }

    private MemberMap(Location location, ITypeSymbol declaredType, IOperation? parameters)
    {
        Parameters = parameters;
        Location = location;
        ElementType = DeclaredType = declaredType;

        if (parameters  is not null && IsCollectionType(declaredType, out var elementType, out var castType)
            && elementType is not null)
        {
            ElementType = elementType;
            CSharpCastType = castType;
            _flags |= MapFlags.IsCollection;
        }
        if (IsMissingOrObjectOrDynamic(ElementType))
        {
            _flags |= MapFlags.IsObject;
        }
        else if (IsDynamicParameters(ElementType))
        {
            _flags |= MapFlags.IsDapperDynamic;
        }
        switch (ChooseConstructor(ElementType, out var constructor))
        {
            case ConstructorResult.SuccessSingleImplicit:
            case ConstructorResult.SuccessSingleExplicit:
                Constructor = constructor;
                break;
        }
        Members = GetMembers(ElementType, constructor);
    }

    static bool IsDynamicParameters(ITypeSymbol? type)
    {
        if (type is null || type.SpecialType != SpecialType.None) return false;
        if (IsBasicDapperType(type, Types.DynamicParameters)
            || IsNestedSqlMapperType(type, Types.IDynamicParameters, TypeKind.Interface)) return true;
        foreach (var i in type.AllInterfaces)
        {
            if (IsNestedSqlMapperType(i, Types.IDynamicParameters, TypeKind.Interface)) return true;
        }
        return false;
    }
}

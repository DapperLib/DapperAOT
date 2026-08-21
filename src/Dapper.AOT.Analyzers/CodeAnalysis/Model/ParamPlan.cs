using Dapper.Internal;
using Microsoft.CodeAnalysis;
using System;
using System.Data;
using static Dapper.Internal.Inspection;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// Everything command-factory emission needs about a parameter type, fully projected at parse
/// time (see the model shape test: no symbols may be cached). Structural equality de-dupes
/// command factories, replacing the old symbol-keyed dictionary.
/// </summary>
internal sealed class ParamPlan : IEquatable<ParamPlan>
{
    public string TypeName { get; } // emitted (Append) form; anonymous renders as the display string
    public string DeclaredType { get; } // "object?" for anonymous types
    public bool IsAnonymous { get; }
    public bool IsReferenceType { get; }
    public bool IsCancellationTokenType { get; }
    public string? ShapeLambda { get; } // the Cast(args, ...) witness, anonymous types only
    public bool IsDynamicBag { get; } // DynamicParameters-style: the factory delegates to the bag itself
    public bool IsCollection { get; } // multi-exec candidate
    public string? CastType { get; }
    public ParamPlan? Element { get; } // multi-exec element (one level only)
    public EquatableArray<ParamMember> Members { get; }

    private ParamPlan(string typeName, string declaredType, bool isAnonymous, bool isReferenceType,
        bool isCancellationTokenType, string? shapeLambda, bool isDynamicBag, bool isCollection, string? castType,
        ParamPlan? element, in EquatableArray<ParamMember> members)
    {
        TypeName = typeName;
        DeclaredType = declaredType;
        IsAnonymous = isAnonymous;
        IsReferenceType = isReferenceType;
        IsCancellationTokenType = isCancellationTokenType;
        ShapeLambda = shapeLambda;
        IsDynamicBag = isDynamicBag;
        IsCollection = isCollection;
        CastType = castType;
        Element = element;
        Members = members;
    }

    public static ParamPlan? Create(ITypeSymbol? type) => Create(type, allowCollection: true);

    private static ParamPlan? Create(ITypeSymbol? type, bool allowCollection)
    {
        if (type is null) return null;

        var typeName = CodeWriter.GetAppendTypeName(type);
        var declaredType = type.IsAnonymousType ? "object?" : CodeWriter.GetTypeName(type);

        string? shapeLambda = null;
        if (type.IsAnonymousType)
        {
            var sb = new CodeWriter();
            AppendShapeLambda(sb, type);
            shapeLambda = sb.ToString();
        }

        bool isDynamicBag = Inspection.IsDynamicParameters(type, out _) && Inspection.HasIdentityFreeAddParameters(type);

        bool isCollection = false;
        string? castType = null;
        ParamPlan? element = null;
        if (!isDynamicBag && allowCollection && Inspection.IsCollectionType(type, out var elementType, out var castTypeValue))
        {
            isCollection = true;
            castType = castTypeValue;
            element = Create(elementType, allowCollection: false);
        }

        EquatableArray<ParamMember> members = default;
        if (!isDynamicBag)
        {
            var memberMap = MemberMap.CreateForParameters(type);
            if (memberMap is not null && !memberMap.Members.IsDefaultOrEmpty)
            {
                var arr = new ParamMember[memberMap.Members.Length];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = ParamMember.Create(memberMap.Members[i]);
                }
                members = new EquatableArray<ParamMember>(arr);
            }
        }

        return new(typeName, declaredType, type.IsAnonymousType, type.IsReferenceType,
            Inspection.IsCancellationToken(type), shapeLambda, isDynamicBag, isCollection, castType, element, members);
    }

    private static void AppendShapeLambda(CodeWriter sb, ITypeSymbol parameterType)
    {
        var members = parameterType.GetMembers();
        int count = CodeWriter.CountGettableInstanceMembers(members);
        switch (count)
        {
            case 0:
                sb.Append("static () => (object?)null");
                break;
            default:
                bool first = true;
                sb.Append("static () => new {");
                foreach (var member in members)
                {
                    if (CodeWriter.IsGettableInstanceMember(member, out var type))
                    {
                        sb.Append(first ? " " : ", ").Append(member.Name).Append(" = default(").Append(type).Append(")");
                        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None)
                        {
                            sb.Append("!");
                        }
                        first = false;
                    }
                }
                sb.Append(" }");
                break;
        }
    }

    public bool Equals(ParamPlan? other) => other is not null
        && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
        && string.Equals(DeclaredType, other.DeclaredType, StringComparison.Ordinal)
        && IsAnonymous == other.IsAnonymous
        && IsReferenceType == other.IsReferenceType
        && IsCancellationTokenType == other.IsCancellationTokenType
        && string.Equals(ShapeLambda, other.ShapeLambda, StringComparison.Ordinal)
        && IsDynamicBag == other.IsDynamicBag
        && IsCollection == other.IsCollection
        && string.Equals(CastType, other.CastType, StringComparison.Ordinal)
        && Equals(Element, other.Element)
        && Members.Equals(other.Members);

    public override bool Equals(object? obj) => Equals(obj as ParamPlan);
    public override int GetHashCode()
    {
        int hash = StringComparer.Ordinal.GetHashCode(TypeName);
        hash = (hash * -47) + Members.GetHashCode();
        return hash;
    }
    public override string ToString() => TypeName;
}

/// <summary>A parameter member as plain data, with the Add-mode sizing decisions precomputed.</summary>
internal readonly struct ParamMember : IEquatable<ParamMember>
{
    public bool IsMapped { get; }
    public bool IsCancellation { get; }
    public bool IsRowCount { get; }
    public string CodeName { get; }
    public string DbName { get; }
    public ParameterDirection Direction { get; }
    public bool IsDbString { get; }
    public bool IsExpandable { get; } // enumerable member: list-expansion (in @ids) applies
    public bool IsCustom { get; } // SqlMapper.ICustomQueryParameter: the value binds itself
    public bool IsValueType { get; } // of the member's own type; decides the null test
    public bool HasDbType { get; } // no DbType => cannot Prepare
    public string? DbTypeName { get; } // for "p.DbType = global::System.Data.DbType.X;"
    public int? EffectiveSize { get; } // after the [n]varchar(max) adjustment
    public bool UseSetValueWithDefaultSize { get; }
    public byte? Precision { get; }
    public byte? Scale { get; }
    public string TypeName { get; } // emitted (Append) form, for Parse<T> in post-process
    public string TypeOfName { get; } // for typeof(...): dynamic becomes object, annotations stripped
    public bool IsEnum { get; } // including Nullable<TEnum>

    private ParamMember(bool isMapped, bool isCancellation, bool isRowCount, string codeName, string dbName,
        ParameterDirection direction, bool isDbString, bool isExpandable, bool isCustom, bool isValueType,
        bool hasDbType, string? dbTypeName, int? effectiveSize,
        bool useSetValueWithDefaultSize, byte? precision, byte? scale, string typeName, string typeOfName,
        bool isEnum)
    {
        IsMapped = isMapped;
        IsCancellation = isCancellation;
        IsRowCount = isRowCount;
        CodeName = codeName;
        DbName = dbName;
        Direction = direction;
        IsDbString = isDbString;
        IsExpandable = isExpandable;
        IsCustom = isCustom;
        IsValueType = isValueType;
        HasDbType = hasDbType;
        DbTypeName = dbTypeName;
        EffectiveSize = effectiveSize;
        UseSetValueWithDefaultSize = useSetValueWithDefaultSize;
        Precision = precision;
        Scale = scale;
        TypeName = typeName;
        TypeOfName = typeOfName;
        IsEnum = isEnum;
    }

    public static ParamMember Create(in ElementMember member)
    {
        if (!member.IsMapped)
        {
            return new(false, false, false, "", "", default, false, false, false, false, false, null, null, false, null, null, "", "", false);
        }
        var dbType = member.GetDbType(out _);
        var size = member.TryGetValue<int>("Size");
        bool useSetValueWithDefaultSize = false;
        if (dbType is not null && size is null)
        {
            switch (dbType.GetValueOrDefault())
            {
                case DbType.Binary:
                case DbType.String:
                case DbType.AnsiString:
                    if (member.CodeType!.SpecialType == SpecialType.System_String)
                    {
                        useSetValueWithDefaultSize = true;
                    }
                    else
                    {
                        size = -1; // default to [n]varchar(max)/varbinary(max)
                    }
                    break;
            }
        }
        return new(true, member.IsCancellation, member.IsRowCount, member.CodeName, member.DbName,
            member.Direction, member.DapperSpecialType is DapperSpecialType.DbString, member.IsExpandable,
            member.DapperSpecialType is DapperSpecialType.CustomQueryParameter, member.CodeType!.IsValueType,
            dbType is not null, dbType?.ToString(), size, useSetValueWithDefaultSize,
            member.TryGetValue<byte>("Precision"), member.TryGetValue<byte>("Scale"),
            CodeWriter.GetAppendTypeName(member.CodeType!),
            member.CodeType!.TypeKind == TypeKind.Dynamic ? "object"
                : CodeWriter.GetAppendTypeName(MakeNonNullable(member.CodeType!)),
            MakeNonNullable(member.CodeType!).TypeKind == TypeKind.Enum);
    }

    public bool Equals(ParamMember other) => IsMapped == other.IsMapped
        && IsCancellation == other.IsCancellation
        && IsRowCount == other.IsRowCount
        && string.Equals(CodeName, other.CodeName, StringComparison.Ordinal)
        && string.Equals(DbName, other.DbName, StringComparison.Ordinal)
        && Direction == other.Direction
        && IsDbString == other.IsDbString
        && IsExpandable == other.IsExpandable
        && IsCustom == other.IsCustom
        && IsValueType == other.IsValueType
        && HasDbType == other.HasDbType
        && string.Equals(DbTypeName, other.DbTypeName, StringComparison.Ordinal)
        && EffectiveSize == other.EffectiveSize
        && UseSetValueWithDefaultSize == other.UseSetValueWithDefaultSize
        && Precision == other.Precision
        && Scale == other.Scale
        && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
        && string.Equals(TypeOfName, other.TypeOfName, StringComparison.Ordinal)
        && IsEnum == other.IsEnum;

    public override bool Equals(object? obj) => obj is ParamMember other && Equals(other);
    public override int GetHashCode() => IsMapped ? StringComparer.Ordinal.GetHashCode(CodeName) : 0;
}

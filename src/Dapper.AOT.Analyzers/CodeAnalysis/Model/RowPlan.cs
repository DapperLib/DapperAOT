using Dapper.Internal;
using Microsoft.CodeAnalysis;
using System;
using static Dapper.Internal.Inspection;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// Everything row-factory emission needs about a result type, fully projected at parse time
/// (see the model shape test: no symbols may be cached). Structural equality is what de-dupes
/// row factories, replacing the old symbol+columns dictionary key.
/// </summary>
internal sealed class RowPlan : IEquatable<RowPlan>
{
    public string TypeName { get; } // emitted (Append) form
    public string NonNullTypeName { get; } // for "T result = new();" when the type was annotated
    public string? InbuiltHelper { get; } // e.g. "Value<int>()": no custom factory is emitted
    public bool UseConstructor { get; }
    public bool UseFactoryMethod { get; }
    public string? FactoryMethodName { get; }
    public bool UseDeferredConstruction { get; }
    public int TotalMemberCount { get; } // the flexible-token offset: counts unmapped members too
    public EquatableArray<RowMember> Members { get; } // the query-column-mapped view
    public EquatableArray<string> QueryColumns { get; }

    private RowPlan(string typeName, string nonNullTypeName, string? inbuiltHelper,
        bool useConstructor, bool useFactoryMethod, string? factoryMethodName, bool useDeferredConstruction,
        int totalMemberCount, in EquatableArray<RowMember> members, in EquatableArray<string> queryColumns)
    {
        TypeName = typeName;
        NonNullTypeName = nonNullTypeName;
        InbuiltHelper = inbuiltHelper;
        UseConstructor = useConstructor;
        UseFactoryMethod = useFactoryMethod;
        FactoryMethodName = factoryMethodName;
        UseDeferredConstruction = useDeferredConstruction;
        TotalMemberCount = totalMemberCount;
        Members = members;
        QueryColumns = queryColumns;
    }

    public static RowPlan? Create(ITypeSymbol? type, in EquatableArray<string> queryColumns)
    {
        if (type is null) return null;

        var typeName = CodeWriter.GetAppendTypeName(type);
        var nonNullTypeName = type.NullableAnnotation == NullableAnnotation.Annotated
            ? CodeWriter.GetAppendTypeName(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated))
            : typeName;

        if (CodeWriter.IsInbuiltResultType(type, out var helper))
        {   // no custom factory will be emitted; the rest is irrelevant
            return new(typeName, nonNullTypeName, helper, false, false, null, false, 0, default, queryColumns);
        }

        var map = MemberMap.CreateForResults(type);
        if (map is null)
        {
            return new(typeName, nonNullTypeName, null, false, false, null, false, 0, default, queryColumns);
        }

        var mapped = map.MapQueryColumns(queryColumns);
        var members = new RowMember[mapped.Length];
        bool hasInitOnly = false, hasRequired = false, hasGetOnly = false;
        for (int i = 0; i < mapped.Length; i++)
        {
            var member = RowMember.Create(mapped[i]);
            members[i] = member;
            if (member.IsMapped)
            {
                hasInitOnly |= member.IsInitOnly;
                hasRequired |= member.IsRequired;
                hasGetOnly |= member is { IsGettable: true, IsSettable: false, IsInitOnly: false };
            }
        }
        bool useConstructor = map.Constructor is not null;
        bool useFactoryMethod = map.FactoryMethod is not null;
        bool useDeferred = useConstructor || useFactoryMethod || hasInitOnly || hasGetOnly || hasRequired;

        return new(typeName, nonNullTypeName, null, useConstructor, useFactoryMethod,
            map.FactoryMethod?.Name, useDeferred, map.Members.Length,
            new EquatableArray<RowMember>(members), queryColumns);
    }

    public bool Equals(RowPlan? other) => other is not null
        && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
        && string.Equals(NonNullTypeName, other.NonNullTypeName, StringComparison.Ordinal)
        && string.Equals(InbuiltHelper, other.InbuiltHelper, StringComparison.Ordinal)
        && UseConstructor == other.UseConstructor
        && UseFactoryMethod == other.UseFactoryMethod
        && string.Equals(FactoryMethodName, other.FactoryMethodName, StringComparison.Ordinal)
        && UseDeferredConstruction == other.UseDeferredConstruction
        && TotalMemberCount == other.TotalMemberCount
        && Members.Equals(other.Members)
        && QueryColumns.Equals(other.QueryColumns);

    public override bool Equals(object? obj) => Equals(obj as RowPlan);
    public override int GetHashCode()
    {
        int hash = StringComparer.Ordinal.GetHashCode(TypeName);
        hash = (hash * -47) + Members.GetHashCode();
        hash = (hash * -47) + QueryColumns.GetHashCode();
        return hash;
    }
    public override string ToString() => TypeName;
}

/// <summary>A result member as plain data (unmapped placeholders keep token positions).</summary>
internal readonly struct RowMember : IEquatable<RowMember>
{
    public bool IsMapped { get; }
    public string CodeName { get; }
    public string DbName { get; }
    public string TypeName { get; } // emitted (Append) form of the member type
    public string NonNullTypeName { get; } // MakeNonNullable form, for GetValue<T> reads
    public string TypeOfName { get; } // for typeof(...) tests: dynamic becomes object (typeof(dynamic) is not legal C#)
    public string AnnotatedTypeName { get; } // GetTypeName(WithNullableAnnotation(Annotated)), for the null-check cast
    public bool CouldBeNullable { get; }
    public string? ReaderMethod { get; } // e.g. GetInt32; null = GetFieldValue<T>
    public int? ConstructorParameterOrder { get; }
    public int? FactoryMethodParameterOrder { get; }
    public bool IsInitOnly { get; }
    public bool IsRequired { get; }
    public bool IsGettable { get; }
    public bool IsSettable { get; }
    public bool NeedsDefaultBang { get; } // reference type, not annotated: "default!"

    private RowMember(bool isMapped, string codeName, string dbName, string typeName, string nonNullTypeName,
        string typeOfName, string annotatedTypeName, bool couldBeNullable, string? readerMethod, int? constructorParameterOrder,
        int? factoryMethodParameterOrder, bool isInitOnly, bool isRequired, bool isGettable, bool isSettable,
        bool needsDefaultBang)
    {
        IsMapped = isMapped;
        CodeName = codeName;
        DbName = dbName;
        TypeName = typeName;
        NonNullTypeName = nonNullTypeName;
        TypeOfName = typeOfName;
        AnnotatedTypeName = annotatedTypeName;
        CouldBeNullable = couldBeNullable;
        ReaderMethod = readerMethod;
        ConstructorParameterOrder = constructorParameterOrder;
        FactoryMethodParameterOrder = factoryMethodParameterOrder;
        IsInitOnly = isInitOnly;
        IsRequired = isRequired;
        IsGettable = isGettable;
        IsSettable = isSettable;
        NeedsDefaultBang = needsDefaultBang;
    }

    public static RowMember Create(in ElementMember member)
    {
        if (!member.IsMapped)
        {
            return new(false, "", "", "", "", "", "", false, null, null, null, false, false, false, false, false);
        }
        var memberType = member.CodeType!;
        member.GetDbType(out var readerMethod);
        var nonNullTypeName = CodeWriter.GetAppendTypeName(Inspection.MakeNonNullable(memberType));
        return new(true, member.CodeName, member.DbName,
            CodeWriter.GetAppendTypeName(memberType),
            nonNullTypeName,
            memberType.TypeKind == TypeKind.Dynamic ? "object" : nonNullTypeName,
            CodeWriter.GetTypeName(memberType.WithNullableAnnotation(NullableAnnotation.Annotated)),
            Inspection.CouldBeNullable(memberType), readerMethod,
            member.ConstructorParameterOrder, member.FactoryMethodParameterOrder,
            member.IsInitOnly, member.IsRequired, member.IsGettable, member.IsSettable,
            memberType.IsReferenceType && memberType.NullableAnnotation == NullableAnnotation.NotAnnotated);
    }

    public bool Equals(RowMember other) => IsMapped == other.IsMapped
        && string.Equals(CodeName, other.CodeName, StringComparison.Ordinal)
        && string.Equals(DbName, other.DbName, StringComparison.Ordinal)
        && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
        && string.Equals(NonNullTypeName, other.NonNullTypeName, StringComparison.Ordinal)
        && string.Equals(TypeOfName, other.TypeOfName, StringComparison.Ordinal)
        && string.Equals(AnnotatedTypeName, other.AnnotatedTypeName, StringComparison.Ordinal)
        && CouldBeNullable == other.CouldBeNullable
        && string.Equals(ReaderMethod, other.ReaderMethod, StringComparison.Ordinal)
        && ConstructorParameterOrder == other.ConstructorParameterOrder
        && FactoryMethodParameterOrder == other.FactoryMethodParameterOrder
        && IsInitOnly == other.IsInitOnly
        && IsRequired == other.IsRequired
        && IsGettable == other.IsGettable
        && IsSettable == other.IsSettable
        && NeedsDefaultBang == other.NeedsDefaultBang;

    public override bool Equals(object? obj) => obj is RowMember other && Equals(other);
    public override int GetHashCode() => IsMapped ? StringComparer.Ordinal.GetHashCode(CodeName) : 0;
}

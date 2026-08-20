using System;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// Plain-data model for one <c>TypeAccessor.CreateAccessor</c>/<c>CreateDataReader</c> call-site:
/// everything the emit step needs, fully projected at parse time. No Roslyn reference types may
/// be stored here (see the model shape test) - a cached symbol pins its whole compilation.
/// </summary>
internal sealed class TypeAccessorModel : IEquatable<TypeAccessorModel>
{
    public LocationSnapshot Location { get; }
    public string ParameterTypeName { get; } // the grouping key; CodeWriter.GetTypeName form
    public bool IsCollection { get; }
    public bool IsPrimitive { get; }
    public EquatableArray<AccessorMember> Members { get; }
    public ForwarderMethod Method { get; }

    public TypeAccessorModel(LocationSnapshot location, string parameterTypeName,
        bool isCollection, bool isPrimitive, EquatableArray<AccessorMember> members, ForwarderMethod method)
    {
        Location = location;
        ParameterTypeName = parameterTypeName;
        IsCollection = isCollection;
        IsPrimitive = isPrimitive;
        Members = members;
        Method = method;
    }

    public bool Equals(TypeAccessorModel? other) => other is not null
        && Location.Equals(other.Location)
        && string.Equals(ParameterTypeName, other.ParameterTypeName, StringComparison.Ordinal)
        && IsCollection == other.IsCollection
        && IsPrimitive == other.IsPrimitive
        && Members.Equals(other.Members)
        && Method.Equals(other.Method);

    public override bool Equals(object? obj) => Equals(obj as TypeAccessorModel);
    public override int GetHashCode()
    {
        int hash = Location.GetHashCode();
        hash = (hash * -47) + StringComparer.Ordinal.GetHashCode(ParameterTypeName);
        hash = (hash * -47) + Members.GetHashCode();
        hash = (hash * -47) + Method.GetHashCode();
        return hash;
    }
}

/// <summary>A gettable+settable member of the accessed type, as plain data.</summary>
internal readonly struct AccessorMember : IEquatable<AccessorMember>
{
    public int Number { get; }
    public bool IsNullable { get; }
    public string Name { get; }
    public string Type { get; } // display form used in emitted code
    public bool IsDBNull { get; } // the member type *is* System.DBNull
    public bool IsSystemObject { get; }
    public string? UnderlyingEnumTypeName { get; } // when the member type is an enum

    public AccessorMember(int number, bool isNullable, string name, string type,
        bool isDbNull, bool isSystemObject, string? underlyingEnumTypeName)
    {
        Number = number;
        IsNullable = isNullable;
        Name = name;
        Type = type;
        IsDBNull = isDbNull;
        IsSystemObject = isSystemObject;
        UnderlyingEnumTypeName = underlyingEnumTypeName;
    }

    public bool Equals(AccessorMember other) => Number == other.Number
        && IsNullable == other.IsNullable
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && string.Equals(Type, other.Type, StringComparison.Ordinal)
        && IsDBNull == other.IsDBNull
        && IsSystemObject == other.IsSystemObject
        && string.Equals(UnderlyingEnumTypeName, other.UnderlyingEnumTypeName, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is AccessorMember other && Equals(other);
    public override int GetHashCode() => (Number * -47) + StringComparer.Ordinal.GetHashCode(Name);
}

/// <summary>The intercepted method's shape, as needed to emit the forwarder.</summary>
internal readonly struct ForwarderMethod : IEquatable<ForwarderMethod>
{
    public string ReturnType { get; }
    public string ContainingType { get; }
    public string Name { get; }
    public EquatableArray<ForwarderParameter> Parameters { get; }

    public ForwarderMethod(string returnType, string containingType, string name, EquatableArray<ForwarderParameter> parameters)
    {
        ReturnType = returnType;
        ContainingType = containingType;
        Name = name;
        Parameters = parameters;
    }

    public bool Equals(ForwarderMethod other)
        => string.Equals(ReturnType, other.ReturnType, StringComparison.Ordinal)
        && string.Equals(ContainingType, other.ContainingType, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && Parameters.Equals(other.Parameters);

    public override bool Equals(object? obj) => obj is ForwarderMethod other && Equals(other);
    public override int GetHashCode()
        => (StringComparer.Ordinal.GetHashCode(Name) * -47) + Parameters.GetHashCode();
}

internal readonly struct ForwarderParameter : IEquatable<ForwarderParameter>
{
    public string Type { get; }
    public string Name { get; }
    public bool IsTypeAccessorParam { get; } // Dapper.TypeAccessor<T>: gets the ?? Instance fallback

    public ForwarderParameter(string type, string name, bool isTypeAccessorParam)
    {
        Type = type;
        Name = name;
        IsTypeAccessorParam = isTypeAccessorParam;
    }

    public bool Equals(ForwarderParameter other)
        => string.Equals(Type, other.Type, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && IsTypeAccessorParam == other.IsTypeAccessorParam;

    public override bool Equals(object? obj) => obj is ForwarderParameter other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
}

/// <summary>
/// The compilation-level facts the emit step needs, projected so the raw
/// <c>Compilation</c> never feeds the output step.
/// </summary>
internal readonly struct GenerationEnvironment : IEquatable<GenerationEnvironment>
{
    public bool AllowUnsafe { get; }
    public string? AssemblyName { get; }
    public bool HasInterceptsLocationAttribute { get; } // already available to the consumer?

    public GenerationEnvironment(bool allowUnsafe, string? assemblyName, bool hasInterceptsLocationAttribute)
    {
        AllowUnsafe = allowUnsafe;
        AssemblyName = assemblyName;
        HasInterceptsLocationAttribute = hasInterceptsLocationAttribute;
    }

    public bool Equals(GenerationEnvironment other) => AllowUnsafe == other.AllowUnsafe
        && string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal)
        && HasInterceptsLocationAttribute == other.HasInterceptsLocationAttribute;

    public override bool Equals(object? obj) => obj is GenerationEnvironment other && Equals(other);
    public override int GetHashCode() => AssemblyName is null ? 0 : StringComparer.Ordinal.GetHashCode(AssemblyName);
}

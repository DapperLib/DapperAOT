using System;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// The intercepted Dapper method's shape as plain data: everything signature emission and
/// parameter forwarding need, projected at parse time (no symbols may be cached - see the
/// model shape test).
/// </summary>
internal sealed class InterceptedMethod : IEquatable<InterceptedMethod>
{
    public string ReturnType { get; } // in emitted (Append) form
    public string Name { get; }
    public bool IsExtension { get; }
    public int Arity { get; }
    /// <summary>Per the NRT shim: the (awaited) return value is not nullable-annotated.</summary>
    public bool ReturnValueNeedsNullForgiving { get; }
    public EquatableArray<MethodParam> Parameters { get; }

    public InterceptedMethod(string returnType, string name, bool isExtension, int arity,
        bool returnValueNeedsNullForgiving, EquatableArray<MethodParam> parameters)
    {
        ReturnType = returnType;
        Name = name;
        IsExtension = isExtension;
        Arity = arity;
        ReturnValueNeedsNullForgiving = returnValueNeedsNullForgiving;
        Parameters = parameters;
    }

    public bool Equals(InterceptedMethod? other) => other is not null
        && string.Equals(ReturnType, other.ReturnType, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal)
        && IsExtension == other.IsExtension
        && Arity == other.Arity
        && ReturnValueNeedsNullForgiving == other.ReturnValueNeedsNullForgiving
        && Parameters.Equals(other.Parameters);

    public override bool Equals(object? obj) => Equals(obj as InterceptedMethod);
    public override int GetHashCode()
    {
        int hash = StringComparer.Ordinal.GetHashCode(Name);
        hash = (hash * -47) + StringComparer.Ordinal.GetHashCode(ReturnType);
        hash = (hash * -47) + Parameters.GetHashCode();
        return hash;
    }
    public override string ToString() => $"{ReturnType} {Name}";
}

internal readonly struct MethodParam : IEquatable<MethodParam>
{
    public string Type { get; } // in emitted (Append) form
    public string Name { get; }

    public MethodParam(string type, string name)
    {
        Type = type;
        Name = name;
    }

    public bool Equals(MethodParam other)
        => string.Equals(Type, other.Type, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is MethodParam other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);
}

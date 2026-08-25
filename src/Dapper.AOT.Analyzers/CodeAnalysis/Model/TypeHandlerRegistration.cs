using System;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// A <c>[TypeHandler(typeof(TValue), typeof(THandler))]</c> registration, fully projected at
/// parse time (see the model shape test: no symbols may be cached).
/// </summary>
internal readonly struct TypeHandlerRegistration : IEquatable<TypeHandlerRegistration>
{
    /// <summary>The handled type, non-nullable, in emitted (Append) form; the match key.</summary>
    public string ValueTypeName { get; }

    /// <summary>The handler type, in emitted (Append) form.</summary>
    public string HandlerTypeName { get; }

    /// <summary>
    /// The handler is a vanilla Dapper <c>SqlMapper.ITypeHandler</c> rather than an
    /// <c>IDbValueHandler&lt;T&gt;</c>, so generated code wraps it in the adapter shim.
    /// </summary>
    public bool IsVanilla { get; }

    public TypeHandlerRegistration(string valueTypeName, string handlerTypeName, bool isVanilla)
    {
        ValueTypeName = valueTypeName;
        HandlerTypeName = handlerTypeName;
        IsVanilla = isVanilla;
    }

    /// <summary>
    /// Find the handler registered for <paramref name="typeName"/> (emitted form, nullability
    /// stripped by the caller), returning the index used to name the emitted static.
    /// </summary>
    public static bool TryFind(in EquatableArray<TypeHandlerRegistration> handlers, string? typeName, out int index)
    {
        if (!string.IsNullOrEmpty(typeName) && !handlers.IsEmpty)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                if (string.Equals(handlers[i].ValueTypeName, typeName, StringComparison.Ordinal))
                {
                    index = i;
                    return true;
                }
            }
        }
        index = -1;
        return false;
    }

    public bool Equals(TypeHandlerRegistration other)
        => string.Equals(ValueTypeName, other.ValueTypeName, StringComparison.Ordinal)
        && string.Equals(HandlerTypeName, other.HandlerTypeName, StringComparison.Ordinal)
        && IsVanilla == other.IsVanilla;

    public override bool Equals(object? obj) => obj is TypeHandlerRegistration other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ValueTypeName);
}

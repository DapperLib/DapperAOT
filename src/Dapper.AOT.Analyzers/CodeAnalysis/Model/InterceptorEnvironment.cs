using System;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// The compilation-level facts the interceptor generator's output step needs, projected so
/// the raw <c>Compilation</c> never feeds that step (which would re-run it on every edit).
/// </summary>
internal sealed class InterceptorEnvironment : IEquatable<InterceptorEnvironment>
{
    public bool AllowUnsafe { get; }
    public string? AssemblyName { get; }
    public bool HasInterceptsLocationAttribute { get; }
    public bool NeedsCommandPrep { get; }
    public string? BaseCommandFactoryName { get; } // [CommandFactory<T>] at module level, if any
    public bool BaseFactoryCanConstruct { get; }
    public EquatableArray<SpecialDbCommandType> SpecialCommandTypes { get; } // providers needing per-command setup
    public ParamPlan SystemObjectPlan { get; } // the parameterless command-factory fallback
    public EquatableArray<TypeHandlerRegistration> TypeHandlers { get; } // [TypeHandler(...)] at module/assembly level

    /// <summary>
    /// Is this project headed for native AOT? Decides whether leaving a call-site on vanilla
    /// Dapper is merely a missed optimization (info) or a latent publish-time crash (warning).
    /// </summary>
    public bool TargetsNativeAot { get; }

    public InterceptorEnvironment(bool allowUnsafe, string? assemblyName, bool hasInterceptsLocationAttribute,
        bool needsCommandPrep, string? baseCommandFactoryName, bool baseFactoryCanConstruct,
        in EquatableArray<SpecialDbCommandType> specialCommandTypes, ParamPlan systemObjectPlan,
        in EquatableArray<TypeHandlerRegistration> typeHandlers, bool targetsNativeAot = false)
    {
        TargetsNativeAot = targetsNativeAot;
        AllowUnsafe = allowUnsafe;
        AssemblyName = assemblyName;
        HasInterceptsLocationAttribute = hasInterceptsLocationAttribute;
        NeedsCommandPrep = needsCommandPrep;
        BaseCommandFactoryName = baseCommandFactoryName;
        BaseFactoryCanConstruct = baseFactoryCanConstruct;
        SpecialCommandTypes = specialCommandTypes;
        SystemObjectPlan = systemObjectPlan;
        TypeHandlers = typeHandlers;
    }

    public bool Equals(InterceptorEnvironment? other) => other is not null
        && AllowUnsafe == other.AllowUnsafe
        && string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal)
        && HasInterceptsLocationAttribute == other.HasInterceptsLocationAttribute
        && NeedsCommandPrep == other.NeedsCommandPrep
        && string.Equals(BaseCommandFactoryName, other.BaseCommandFactoryName, StringComparison.Ordinal)
        && BaseFactoryCanConstruct == other.BaseFactoryCanConstruct
        && SpecialCommandTypes.Equals(other.SpecialCommandTypes)
        && SystemObjectPlan.Equals(other.SystemObjectPlan)
        && TypeHandlers.Equals(other.TypeHandlers)
        && TargetsNativeAot == other.TargetsNativeAot;

    public override bool Equals(object? obj) => Equals(obj as InterceptorEnvironment);
    public override int GetHashCode()
        => (AssemblyName is null ? 0 : StringComparer.Ordinal.GetHashCode(AssemblyName))
        ^ SpecialCommandTypes.GetHashCode();
}

/// <summary>A provider command type that needs special per-command initialization.</summary>
internal readonly struct SpecialDbCommandType : IEquatable<SpecialDbCommandType>
{
    public string TypeName { get; } // emitted (Append) form
    public string ShortName { get; } // for the comment
    public bool BindByName { get; }
    public bool InitialLONGFetchSize { get; }

    public SpecialDbCommandType(string typeName, string shortName, bool bindByName, bool initialLongFetchSize)
    {
        TypeName = typeName;
        ShortName = shortName;
        BindByName = bindByName;
        InitialLONGFetchSize = initialLongFetchSize;
    }

    public bool Equals(SpecialDbCommandType other)
        => string.Equals(TypeName, other.TypeName, StringComparison.Ordinal)
        && string.Equals(ShortName, other.ShortName, StringComparison.Ordinal)
        && BindByName == other.BindByName
        && InitialLONGFetchSize == other.InitialLONGFetchSize;

    public override bool Equals(object? obj) => obj is SpecialDbCommandType other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(TypeName);
}

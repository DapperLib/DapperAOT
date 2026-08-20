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
    public bool HasModuleInitializer { get; } // ModuleInitializerAttribute available (net5+, or consumer polyfill)
    public bool HasVanillaTypeHandlers { get; } // SqlMapper.HasTypeHandler/LookupDbType present in the referenced Dapper
    public bool NeedsCommandPrep { get; }
    public string? BaseCommandFactoryName { get; } // [CommandFactory<T>] at module level, if any
    public bool BaseFactoryCanConstruct { get; }
    public EquatableArray<SpecialDbCommandType> SpecialCommandTypes { get; } // providers needing per-command setup
    public ParamPlan SystemObjectPlan { get; } // the parameterless command-factory fallback

    public InterceptorEnvironment(bool allowUnsafe, string? assemblyName, bool hasInterceptsLocationAttribute,
        bool hasModuleInitializer, bool hasVanillaTypeHandlers,
        bool needsCommandPrep, string? baseCommandFactoryName, bool baseFactoryCanConstruct,
        in EquatableArray<SpecialDbCommandType> specialCommandTypes, ParamPlan systemObjectPlan)
    {
        AllowUnsafe = allowUnsafe;
        AssemblyName = assemblyName;
        HasInterceptsLocationAttribute = hasInterceptsLocationAttribute;
        HasModuleInitializer = hasModuleInitializer;
        HasVanillaTypeHandlers = hasVanillaTypeHandlers;
        NeedsCommandPrep = needsCommandPrep;
        BaseCommandFactoryName = baseCommandFactoryName;
        BaseFactoryCanConstruct = baseFactoryCanConstruct;
        SpecialCommandTypes = specialCommandTypes;
        SystemObjectPlan = systemObjectPlan;
    }

    public bool Equals(InterceptorEnvironment? other) => other is not null
        && AllowUnsafe == other.AllowUnsafe
        && string.Equals(AssemblyName, other.AssemblyName, StringComparison.Ordinal)
        && HasInterceptsLocationAttribute == other.HasInterceptsLocationAttribute
        && HasModuleInitializer == other.HasModuleInitializer
        && HasVanillaTypeHandlers == other.HasVanillaTypeHandlers
        && NeedsCommandPrep == other.NeedsCommandPrep
        && string.Equals(BaseCommandFactoryName, other.BaseCommandFactoryName, StringComparison.Ordinal)
        && BaseFactoryCanConstruct == other.BaseFactoryCanConstruct
        && SpecialCommandTypes.Equals(other.SpecialCommandTypes)
        && SystemObjectPlan.Equals(other.SystemObjectPlan);

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

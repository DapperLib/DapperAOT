using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Dapper.Internal;

internal readonly struct CommandProperty : IEquatable<CommandProperty>
{
    public readonly INamedTypeSymbol CommandType;
    public readonly string Name;
    public readonly object Value;
    public readonly Location? Location;

    public CommandProperty(INamedTypeSymbol commandType, string name, object value, Location? location)
    {
        CommandType = commandType;
        Name = name;
        Value = value;
        Location = location;
    }

    public override string ToString() => $"{CommandType.Name}.{Name}={Value}";

    public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(CommandType) & Name.GetHashCode() ^ (Value?.GetHashCode() ?? 0) ^ (Location?.GetHashCode() ?? 0);

    public override bool Equals(object obj) => obj is CommandProperty other && Equals(in other);

    bool IEquatable<CommandProperty>.Equals(CommandProperty other) => Equals(in other);
    public bool Equals(in CommandProperty other)
        => SymbolEqualityComparer.Default.Equals(CommandType, other.CommandType) && string.Equals(Name, other.Name) && Equals(Value, other.Value) && Equals(Location, other.Location);
}

internal sealed class AdditionalCommandState : IEquatable<AdditionalCommandState>
{
    public readonly int EstimatedRowCount;
    public readonly string? EstimatedRowCountMemberName;
    public readonly ImmutableArray<CommandProperty> CommandProperties;

    public bool HasEstimatedRowCount => EstimatedRowCount > 0 || EstimatedRowCountMemberName is not null;

    public bool HasCommandProperties => !CommandProperties.IsDefaultOrEmpty;

    public static AdditionalCommandState? Parse(ISymbol? target, ITypeSymbol? parameterType, ref object? diagnostics)
    {
        if (target is null) return null;

        var inherited = target is IAssemblySymbol ? null : Parse(target.ContainingSymbol, parameterType, ref diagnostics);
        var local = ReadFor(target, parameterType, ref diagnostics);
        if (inherited is null) return local;
        if (local is null) return inherited;
        return Combine(inherited, local);
    }

    private static AdditionalCommandState? ReadFor(ISymbol target, ITypeSymbol? parameterType, ref object? diagnostics)
    {
        var attribs = target.GetAttributes();
        if (attribs.IsDefaultOrEmpty) return null;

        int cmdPropsCount = 0;
        int estimatedRowCount = 0;
        string? estimatedRowCountMember = null;
        if (parameterType is not null)
        {
            foreach (var member in Inspection.GetMembers(parameterType))
            {
                if (member.IsEstimatedRowCount)
                {
                    if (estimatedRowCountMember is not null)
                    {
                        DiagnosticsBase.Add(ref diagnostics, Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MemberRowCountHintDuplicated, member.GetLocation()));
                    }
                    estimatedRowCountMember = member.Member.Name;
                }
            }
        }
        var location = target.Locations.FirstOrDefault();
        foreach (var attrib in attribs)
        {
            if (Inspection.IsDapperAttribute(attrib))
            {
                switch (attrib.AttributeClass!.Name)
                {
                    case Types.EstimatedRowCountAttribute:
                        if (attrib.ConstructorArguments.Length == 1 && attrib.ConstructorArguments[0].Value is int i
                            && i > 0)
                        {
                            if (estimatedRowCountMember is null)
                            {
                                estimatedRowCount = i;
                            }
                            else
                            {
                                DiagnosticsBase.Add(ref diagnostics, Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MethodRowCountHintRedundant, location, estimatedRowCountMember));
                            }
                        }
                        else
                        {
                            DiagnosticsBase.Add(ref diagnostics, Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MethodRowCountHintInvalid, location));
                        }
                        break;
                    case Types.CommandPropertyAttribute:
                        cmdPropsCount++;
                        break;
                }
            }
        }

        ImmutableArray<CommandProperty> cmdProps;
        if (cmdPropsCount != 0)
        {
            var builder = ImmutableArray.CreateBuilder<CommandProperty>(cmdPropsCount);
            foreach (var attrib in attribs)
            {
                if (Inspection.IsDapperAttribute(attrib) && attrib.AttributeClass!.Name == Types.CommandPropertyAttribute
                    && attrib.AttributeClass.Arity == 1
                    && attrib.AttributeClass.TypeArguments[0] is INamedTypeSymbol cmdType
                    && attrib.ConstructorArguments.Length == 2
                    && attrib.ConstructorArguments[0].Value is string name
                    && attrib.ConstructorArguments[1].Value is object value)
                {
                    builder.Add(new(cmdType, name, value, location));
                }
            }
            cmdProps = builder.ToImmutable();
        }
        else
        {
            cmdProps = ImmutableArray<CommandProperty>.Empty;
        }


        return cmdProps.IsDefaultOrEmpty && estimatedRowCount <= 0 && estimatedRowCountMember is null
            ? null : new(estimatedRowCount, estimatedRowCountMember, cmdProps);
    }

    private static AdditionalCommandState Combine(AdditionalCommandState inherited, AdditionalCommandState overrides)
    {
        if (inherited is null) return overrides;
        if (overrides is null) return inherited;

        var count = inherited.EstimatedRowCount;
        var countMember = inherited.EstimatedRowCountMemberName;

        if (overrides.EstimatedRowCountMemberName is not null)
        {
            count = 0;
            countMember = overrides.EstimatedRowCountMemberName;
        }
        else if (overrides.EstimatedRowCount > 0)
        {
            count = overrides.EstimatedRowCount;
            countMember = null;
        }

        return new(count, countMember, Concat(inherited.CommandProperties, overrides.CommandProperties));
    }

    static ImmutableArray<CommandProperty> Concat(ImmutableArray<CommandProperty> x, ImmutableArray<CommandProperty> y)
    {
        if (x.IsDefaultOrEmpty) return y;
        if (y.IsDefaultOrEmpty) return x;
        var builder = ImmutableArray.CreateBuilder<CommandProperty>(x.Length + y.Length);
        builder.AddRange(x);
        builder.AddRange(y);
        return builder.ToImmutable();
    }

    private AdditionalCommandState(int estimatedRowCount, string? estimatedRowCountMemberName, ImmutableArray<CommandProperty> commandProperties)
    {
        EstimatedRowCount = estimatedRowCount;
        EstimatedRowCountMemberName = estimatedRowCountMemberName;
        CommandProperties = commandProperties;
    }


    public override bool Equals(object obj) => obj is AdditionalCommandState other && Equals(in other);

    bool IEquatable<AdditionalCommandState>.Equals(AdditionalCommandState other) => Equals(in other);

    public bool Equals(in AdditionalCommandState other)
        => EstimatedRowCount == other.EstimatedRowCount && EstimatedRowCountMemberName == other.EstimatedRowCountMemberName
        && ((CommandProperties.IsDefaultOrEmpty && other.CommandProperties.IsDefaultOrEmpty) || Equals(CommandProperties, other.CommandProperties));

    private static bool Equals(in ImmutableArray<CommandProperty> x, in ImmutableArray<CommandProperty> y)
    {
        if (x.IsDefaultOrEmpty)
        {
            return y.IsDefaultOrEmpty;
        }
        if (y.IsDefaultOrEmpty || x.Length != y.Length)
        {
            return false;
        }
        var ySpan = y.AsSpan();
        int index = 0;
        foreach (ref readonly CommandProperty xVal in x.AsSpan())
        {
            if (!xVal.Equals(in ySpan[index++]))
            {
                return false;
            }
        }
        return true;
    }

    static int GetHashCode(in ImmutableArray<CommandProperty> x)
    {
        if (x.IsDefaultOrEmpty) return 0;

        var value = 0;
        foreach (ref readonly CommandProperty xVal in x.AsSpan())
        {
            value = (value * -42) + xVal.GetHashCode();
        }
        return value;
    }

    public override int GetHashCode()
        => (EstimatedRowCount + (EstimatedRowCountMemberName is null ? 0 : EstimatedRowCountMemberName.GetHashCode()))
        ^ (CommandProperties.IsDefaultOrEmpty ? 0 : GetHashCode(in CommandProperties));
}

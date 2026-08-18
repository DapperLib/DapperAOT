using Dapper.CodeAnalysis;
using Dapper.Internal;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis.Model;

/// <summary>
/// A <c>[CommandProperty]</c> declaration as plain data (this rides in the cached generator
/// model, so no symbols may be stored - see the model shape test); the validity probes that
/// need the command-type symbol run at construction.
/// </summary>
internal readonly struct CommandProperty : IEquatable<CommandProperty>
{
    public readonly string CommandTypeName; // emitted (Append) form, for the "cmd is X" test
    public readonly string CommandTypeShortName; // for diagnostics
    public readonly bool IsDbCommand; // System.Data.Common.DbCommand itself: no type test needed
    public readonly bool MemberExists; // the named member probe, evaluated against the symbol
    public readonly string Name;
    public readonly object Value; // attribute constant: string/int/bool etc
    public readonly LocationSnapshot Location;

    private CommandProperty(string commandTypeName, string commandTypeShortName, bool isDbCommand,
        bool memberExists, string name, object value, in LocationSnapshot location)
    {
        CommandTypeName = commandTypeName;
        CommandTypeShortName = commandTypeShortName;
        IsDbCommand = isDbCommand;
        MemberExists = memberExists;
        Name = name;
        Value = value;
        Location = location;
    }

    public static CommandProperty Create(INamedTypeSymbol commandType, string name, object value, Location? location)
    {
        bool isDbCmd = commandType is
        {
            Name: "DbCommand", ContainingType: null, Arity: 0, TypeKind: TypeKind.Class, ContainingNamespace:
            {
                Name: "Common",
                ContainingNamespace:
                {
                    Name: "Data",
                    ContainingNamespace:
                    {
                        Name: "System",
                        ContainingNamespace.IsGlobalNamespace: true
                    }
                }
            }
        };
        return new(CodeWriter.GetAppendTypeName(commandType), commandType.Name, isDbCmd,
            HasPublicSettableInstanceMember(commandType, name), name, value,
            location is null ? default : new LocationSnapshot(location));
    }

    private static bool HasPublicSettableInstanceMember(ITypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic || member.Name != name || member.DeclaredAccessibility != Accessibility.Public) continue;
            return member.Kind switch
            {
                SymbolKind.Field when member is IFieldSymbol field => !field.IsReadOnly,
                SymbolKind.Property when member is IPropertySymbol prop => prop.SetMethod is not null,
                _ => false,
            };
        }
        return false;
    }

    public override string ToString() => $"{CommandTypeShortName}.{Name}={Value}";

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(CommandTypeName) & Name.GetHashCode() ^ (Value?.GetHashCode() ?? 0) ^ Location.GetHashCode();

    public override bool Equals(object obj) => obj is CommandProperty other && Equals(in other);

    bool IEquatable<CommandProperty>.Equals(CommandProperty other) => Equals(in other);
    public bool Equals(in CommandProperty other)
        => string.Equals(CommandTypeName, other.CommandTypeName, StringComparison.Ordinal)
        && IsDbCommand == other.IsDbCommand
        && MemberExists == other.MemberExists
        && string.Equals(Name, other.Name)
        && Equals(Value, other.Value)
        && Location.Equals(other.Location);
}

internal sealed class AdditionalCommandState : IEquatable<AdditionalCommandState>
{
    public readonly int RowCountHint;
    public readonly int? BatchSize;
    public readonly string? RowCountHintMemberName;
    public readonly EquatableArray<CommandProperty> CommandProperties;
    public readonly EquatableArray<string> QueryColumns; // default (unset) is distinct from empty

    public bool HasRowCountHint => RowCountHint > 0 || RowCountHintMemberName is not null;

    public bool HasCommandProperties => !CommandProperties.IsEmpty;

    public static AdditionalCommandState? Parse(ISymbol? target, MemberMap? map, Action<Diagnostic>? reportDiagnostic)
    {
        if (target is null) return null;

        var inherited = target is IAssemblySymbol ? null : Parse(target.ContainingSymbol, null, reportDiagnostic);
        var local = DapperAnalyzer.SharedGetAdditionalCommandState(target, map, reportDiagnostic);
        if (inherited is null) return local;
        if (local is null) return inherited;
        return Combine(inherited, local);
    }

    private static AdditionalCommandState Combine(AdditionalCommandState inherited, AdditionalCommandState overrides)
    {
        if (inherited is null) return overrides;
        if (overrides is null) return inherited;

        var count = inherited.RowCountHint;
        var countMember = inherited.RowCountHintMemberName;

        if (overrides.RowCountHintMemberName is not null)
        {
            count = 0;
            countMember = overrides.RowCountHintMemberName;
        }
        else if (overrides.RowCountHint > 0)
        {
            count = overrides.RowCountHint;
            countMember = null;
        }

        return new(count, countMember, inherited.BatchSize ?? overrides.BatchSize,
            Concat(inherited.CommandProperties, overrides.CommandProperties),
            overrides.QueryColumns.IsDefault ? inherited.QueryColumns : overrides.QueryColumns);
    }

    static EquatableArray<CommandProperty> Concat(in EquatableArray<CommandProperty> x, in EquatableArray<CommandProperty> y)
    {
        if (x.IsEmpty) return y;
        if (y.IsEmpty) return x;
        var arr = new CommandProperty[x.Length + y.Length];
        int index = 0;
        foreach (var item in x) arr[index++] = item;
        foreach (var item in y) arr[index++] = item;
        return new(arr);
    }

    internal AdditionalCommandState(
        int rowCountHint, string? rowCountHintMemberName, int? batchSize,
        in EquatableArray<CommandProperty> commandProperties, in EquatableArray<string> queryColumns)
    {
        RowCountHint = rowCountHint;
        RowCountHintMemberName = rowCountHintMemberName;
        BatchSize = batchSize;
        CommandProperties = commandProperties;
        QueryColumns = queryColumns;
    }

    public override bool Equals(object obj) => obj is AdditionalCommandState other && Equals(in other);

    bool IEquatable<AdditionalCommandState>.Equals(AdditionalCommandState other) => Equals(in other);

    public bool Equals(in AdditionalCommandState other)
        => RowCountHint == other.RowCountHint
        && BatchSize == other.BatchSize
        && RowCountHintMemberName == other.RowCountHintMemberName
        && CommandProperties.Equals(other.CommandProperties)
        && QueryColumns.Equals(other.QueryColumns);

    public override int GetHashCode()
        => (RowCountHint + BatchSize.GetValueOrDefault()
        + (RowCountHintMemberName is null ? 0 : StringComparer.Ordinal.GetHashCode(RowCountHintMemberName)))
        ^ CommandProperties.GetHashCode() ^ QueryColumns.GetHashCode();
}

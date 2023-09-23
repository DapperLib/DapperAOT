using System;
using System.Collections.Immutable;
using System.Data;
using static Dapper.Internal.Inspection;
using static Dapper.Internal.SqlParameter;

namespace Dapper.Internal;

internal readonly struct SqlParameter
{
    public readonly string Name { get; }
    public readonly ParameterDirection Direction { get; }

    private readonly ParameterFlags flags;
    public SqlParameter(string name, ParameterDirection direction = ParameterDirection.Input, ParameterFlags flags = ParameterFlags.None)
    {
        Name = name;
        Direction = direction;
        this.flags = flags;
    }

    public bool IsExpandable => (flags & ParameterFlags.IsExpandable) != 0;
    public bool IsTable => (flags & ParameterFlags.IsTable) != 0;

    [Flags]
    public enum ParameterFlags
    {
        None = 0,
        IsExpandable = 1 << 0,
        IsTable = 1 << 1,
    }

    
}
internal static class SqlParameters
{
    public static ImmutableArray<SqlParameter> None => ImmutableArray<SqlParameter>.Empty;
    public static ImmutableArray<SqlParameter> From(ImmutableArray<ElementMember>? parameters)
    {
        if (parameters is null) return None;
        var args = parameters.GetValueOrDefault();
        if (args.IsDefaultOrEmpty) return None;

        var builder = ImmutableArray.CreateBuilder<SqlParameter>(args.Length);
        foreach (var arg in args)
        {
            if (arg.Kind == ElementMemberKind.None)
            {
                var flags = arg.IsExpandable ? ParameterFlags.IsExpandable : ParameterFlags.None;
                builder.Add(new(arg.DbName, arg.Direction, flags));
            }
        }
        return builder.ToImmutable();
    }
}

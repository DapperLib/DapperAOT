using System;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Defines additional provider-specific configuration to apply to commands
/// </summary>
/// <typeparam name="T">The type of command to which this member applies (use <see cref="DbCommand"/> to apply to all commands)</typeparam>
/// <remarks>This can be used to provide granular (location-specific) or global command configuration</remarks>
[ImmutableObject(true)]
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[AttributeUsage(AttributeTargets.Module | AttributeTargets.Method, AllowMultiple = true)]
public sealed class CommandPropertyAttribute<T> : Attribute where T : DbCommand
{
    /// <summary>
    /// Defines additional provider-specific configuration rules to apply to commands
    /// </summary>
    /// <param name="name">The name of the member to assign; consider using <c>nameof</c> here, to avoid errors</param>
    /// <param name="value">The value of the member to assign</param>
    /// <remarks>This can be used to provide granular (location-specific) or global command configuration</remarks>
    public CommandPropertyAttribute(string name, bool value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Defines additional provider-specific configuration rules to apply to commands
    /// </summary>
    /// <param name="name">The name of the member to assign; consider using <c>nameof</c> here, to avoid errors</param>
    /// <param name="value">The value of the member to assign</param>
    /// <remarks>This can be used to provide granular (location-specific) or global command configuration</remarks>
    public CommandPropertyAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Defines additional provider-specific configuration rules to apply to commands
    /// </summary>
    /// <param name="name">The name of the member to assign; consider using <c>nameof</c> here, to avoid errors</param>
    /// <param name="value">The value of the member to assign</param>
    /// <remarks>This can be used to provide granular (location-specific) or global command configuration</remarks>
    public CommandPropertyAttribute(string name, int value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// The name of the member to assign; consider using <c>nameof</c> here, to avoid errors
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The value of the member to assign
    /// </summary>
    public object? Value { get; }
}

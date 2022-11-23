using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Dapper;

/// <summary>
/// Indicates that a method represents a command, or that a parameter is the command text
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
[Conditional("DEBUG")]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// The SQL to use for this command.
    /// </summary>
    public string? CommandText { get; }

    /// <summary>
    /// Creates a new <see cref="CommandAttribute"/> instance with the specified <see cref="CommandText"/>.
    /// </summary>
    public CommandAttribute(string commandText)
        => CommandText = commandText;

    /// <summary>
    /// Creates a new <see cref="CommandAttribute"/> instance with the specified <see cref="CommandText"/> and <see cref="CommandType"/>.
    /// </summary>
    public CommandAttribute(string commandText, CommandType commandType)
        : this(commandText) => CommandType = commandType;

    /// <summary>
    /// The <see cref="CommandType"/> to use for this operation (if omitted, uses <see cref="CommandType.Text"/> for commands with a space character, or <see cref="CommandType.StoredProcedure"/> otherwise).
    /// </summary>
    public CommandType CommandType { get; set; }

    /// <summary>
    /// Creates a new <see cref="CommandAttribute"/> instance.
    /// </summary>
    public CommandAttribute() { }
}

/// <summary>
/// Indicates whether to prepend fixed command-text with origin hints (useful for debugging, profiling, etc)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
    | AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
[Conditional("DEBUG")]
public sealed class AnnotateAttribute : Attribute
{
    /// <summary>
    /// Whether to prepend fixed command-text with origin hints (useful for debugging, profiling, etc)
    /// </summary>
    public bool Annotate { get; }
    /// <summary>
    /// Indicates whether to prepend fixed command-text with origin hints (useful for debugging, profiling, etc)
    /// </summary>
    public AnnotateAttribute(bool annotate = true)
        => Annotate = annotate;
}

/// <summary>
/// Indicates whether to re-use <see cref="DbCommand"/> instances between operations.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface
    | AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
[Conditional("DEBUG")]
public sealed class ReuseCommandAttribute : Attribute
{
    /// <summary>
    /// Whether to re-use <see cref="DbCommand"/> instances between operations.
    /// </summary>
    public bool ReuseCommand { get; }

    /// <summary>
    /// Indicates whether to prepend fixed command-text with origin hints (useful for debugging, profiling, etc)
    /// </summary>
    public ReuseCommandAttribute(bool reuseCommand = true)
        => ReuseCommand = reuseCommand;
}

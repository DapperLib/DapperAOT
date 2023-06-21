using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Specifies additional metadata to be used for columns and parameter handling; only explicitly specified values are used
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class DbValueAttribute
    : Attribute
{
    /// <summary>
    /// Gets or sets whether to ignore this member
    /// </summary>
    public bool Ignore { get; set; }
    /// <summary>
    /// Gets or sets the override name used for this value as a column or parameter
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the <see cref="DbType"/> to be used for this value as a column or parameter
    /// </summary>
    public DbType DbType { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbParameter.Direction"/> for this value as a parameter
    /// </summary>
    public ParameterDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbParameter.Precision"/> for this value as a parameter
    /// </summary>
    public byte Precision { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbParameter.Scale"/> for this value as a parameter
    /// </summary>
    public byte Scale { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbParameter.Size"/> (<b>in bytes</b>) for this value as a parameter
    /// </summary>
    public int Size { get; set; }
}

/// <summary>
/// Specifies additional metadata to be used for parameters with specific provider types
/// </summary>
/// <typeparam name="T">The type of provider parameter that this metadata applies to</typeparam>
/// <remarks>Essentially, this generates:<code>
/// if (parameter is {T} typed)
/// {
///     parameter.{memberName} = {value};
/// }
/// </code></remarks>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class DbAttribute<T> : Attribute where T : DbParameter
{
    /// <summary>
    /// Creates a new metadata entry that applies to parameters of type <typeparamref name="T"/>
    /// </summary>
    /// <param name="memberName">The name of the parameter property to set</param>
    /// <param name="value">The value to assign to the parameter property</param>
    public DbAttribute(string memberName, object value)
    {
        _ = memberName;
        _ = value;
    }
}
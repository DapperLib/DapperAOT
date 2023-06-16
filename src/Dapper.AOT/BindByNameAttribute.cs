using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether name-based result-binding for value tuples is enabled for this element
/// </summary>
[ImmutableObject(true), Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class BindByNameAttribute : Attribute
{
    /// <summary>
    /// Indicates whether name-based result-binding for value tuples is enabled for this element
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Indicates whether name-based result-binding for value tuples is enabled for this element
    /// </summary>
    public BindByNameAttribute(bool enabled = true) => Enabled = enabled;
}

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether Dapper AOT is enabled for this element
/// </summary>
[ImmutableObject(true), Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class DapperAotAttribute : Attribute
{
    /// <summary>
    /// Indicates whether Dapper AOT is enabled for this element
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Indicates whether Dapper AOT is enabled for this element
    /// </summary>
    public DapperAotAttribute(bool enabled = true) => Enabled = enabled;
}

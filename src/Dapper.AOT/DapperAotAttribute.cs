using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether Dapper AOT is enabled for this element
/// </summary>
[ImmutableObject(true), Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DapperAotAttribute : Attribute
{
    /// <summary>
    /// Indicates whether Dapper AOT is enabled for this element
    /// </summary>
    public bool Enabled { get; set; }
    /// <summary>
    /// Indicates whether Dapper AOT is enabled for this element
    /// </summary>
    public DapperAotAttribute(bool enabled) => Enabled = enabled;
}

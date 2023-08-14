using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether to include, when possible, a comment in the SQL showing the code origin
/// </summary>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class IncludeLocationAttribute : Attribute
{
    /// <summary>
    /// Indicates whether to include, when possible, a comment in the SQL showing the code origin
    /// </summary>
    public IncludeLocationAttribute(bool enabled = true) => _ = enabled;
}

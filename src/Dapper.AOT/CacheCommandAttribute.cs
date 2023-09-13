using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether commands should, when possible, be re-used between operations
/// </summary>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class CacheCommandAttribute : Attribute
{
    /// <summary>
    /// Indicates whether commands should, when possible, be re-used between operations
    /// </summary>
    public CacheCommandAttribute(bool enabled = true) => _ = enabled;
}

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates whether to include, when possible, a comment in the SQL showing the code origin
/// </summary>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class AnnotateSqlSourceAttribute : Attribute
{
    /// <summary>
    /// Indicates whether to include, when possible, a comment in the SQL showing the code origin
    /// </summary>
    public AnnotateSqlSourceAttribute(bool enabled = true) { }
}

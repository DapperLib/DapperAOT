using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Binds values using strict typing; no type conversions will be applied.
/// </summary>
[ImmutableObject(true), Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
public sealed class StrictTypesAttribute(bool enabled = true) : Attribute
{
    /// <summary>
    /// Gets whether strict typing is enabled.
    /// </summary>
    public bool Enabled { get; } = enabled;
}
using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Provides the expected number of records from a query.
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class EstimatedRowCountAttribute : Attribute
{
    /// <summary>
    /// Provides the expected number of records from a query.
    /// </summary>
    /// <param name="count">The number of records expected (ignored if this is a field/property)</param>
    public EstimatedRowCountAttribute(int count) => _ = count;

    /// <summary>
    /// Indicates a member that provides the expected number of records from a query.
    /// </summary>
    public EstimatedRowCountAttribute() { }
}

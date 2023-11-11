using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Provides the expected number of records from a query; this can help with optimizations such as buffer allocation.
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class RowCountHintAttribute : Attribute
{
    /// <summary>
    /// Provides the expected number of records from a query.
    /// </summary>
    /// <param name="count">The number of records expected (ignored if this is a field/property)</param>
    public RowCountHintAttribute(int count) => _ = count;

    /// <summary>
    /// Indicates a member that provides the expected number of records from a query.
    /// </summary>
    public RowCountHintAttribute() { }
}

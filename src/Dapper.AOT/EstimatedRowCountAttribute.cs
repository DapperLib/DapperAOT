using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Provides the expected number of records from a query.
/// </summary>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public sealed class EstimatedRowCountAttribute : Attribute
{
    /// <summary>
    /// Provides the expected number of records from a query.
    /// </summary>
    /// <param name="count">The number of records expected (ignored if this is a field/property)</param>
    /// <param name="resultIndex">The result set to which this value relates</param>
    public EstimatedRowCountAttribute(int count, int resultIndex = 0) { }
}

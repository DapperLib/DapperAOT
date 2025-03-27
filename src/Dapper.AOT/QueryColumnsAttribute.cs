using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Specifies the ordered columns returned by a query.
/// </summary>
/// <param name="columns"></param>
[AttributeUsage(AttributeTargets.Method)]
[ImmutableObject(true), Conditional("DEBUG")]
public sealed class QueryColumnsAttribute(params string[] columns) : Attribute
{
    /// <summary>
    /// The ordered columns returned by a query.
    /// </summary>
    public ImmutableArray<string> Columns { get; } = columns.ToImmutableArray();
}
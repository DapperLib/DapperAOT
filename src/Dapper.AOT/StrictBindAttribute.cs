using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Binds using strict positional rules, without any type mapping; values are expected to be exact.
/// </summary>
/// <param name="columns">The ordered columns bound by this operation.</param>
[AttributeUsage(AttributeTargets.Method)] // can open up later if we add SQL parsing
[ImmutableObject(true), Conditional("DEBUG")]
public sealed class StrictBindAttribute(params string?[] columns) : Attribute
{
    private readonly string?[] _columns = columns;

    /// <summary>
    /// The ordered columns bound by this operation.
    /// </summary>
    public ReadOnlySpan<string?> Columns => _columns;
}


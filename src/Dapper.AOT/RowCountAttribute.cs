using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Specifies additional metadata to be used for columns and parameter handling; only explicitly specified values are used
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class RowCountAttribute : Attribute { }
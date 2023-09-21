using System;
using System.ComponentModel;

namespace Dapper;

/// <summary>
/// Indicates that a value should be interpreted as SQL
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
public sealed class SqlAttribute : Attribute { }

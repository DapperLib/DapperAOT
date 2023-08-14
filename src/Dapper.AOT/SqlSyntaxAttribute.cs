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
public sealed class SqlSyntaxAttribute : Attribute
{
    /// <summary>
    /// Indicates whether commands should, when possible, be re-used between operations
    /// </summary>
    public SqlSyntaxAttribute(SqlSyntax syntax) => _ = syntax;
}
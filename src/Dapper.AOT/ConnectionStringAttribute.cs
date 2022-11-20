using System;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Provides a connection-source to be used with methods that control their own connections internally
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
[Conditional("DEBUG")]
public sealed class ConnectionAttribute : Attribute
{
}

/// <summary>
/// Provides a connection-string to be used with methods that control their own connections internally
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
[Conditional("DEBUG")]
public sealed class ConnectionStringAttribute : Attribute
{
}

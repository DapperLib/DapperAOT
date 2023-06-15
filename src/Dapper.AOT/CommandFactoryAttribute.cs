using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Allows a base command-factory to be specified, applying any implementation-specific logic required;
/// the 'object' here is a placeholder, and will be substituted on a per-argument basis
/// </summary>
[ImmutableObject(true)]
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[AttributeUsage(AttributeTargets.Module, AllowMultiple = false)]
public sealed class CommandFactoryAttribute<T> : Attribute where T : CommandFactory<object>
{
}

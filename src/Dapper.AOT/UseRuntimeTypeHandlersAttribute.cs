using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates that generated code should honor type-handlers registered at runtime through
/// <c>SqlMapper.AddTypeHandler</c>, by deferring to Dapper's own decision procedure when it
/// executes. Off by default: registrations that only exist at runtime cannot be seen by the
/// generator, cannot be verified at build time, and reach machinery that native AOT cannot
/// resolve - declare handlers instead, and use this while migrating.
/// </summary>
/// <remarks>
/// Deliberately assembly/module scope only. A handler registration is a property of a
/// <i>type</i>, so it cuts across every call-site that touches that type; allowing it per
/// method or per class would let one type bind two different ways in one process.
/// </remarks>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = false)]
public sealed class UseRuntimeTypeHandlersAttribute : Attribute
{
    /// <summary>
    /// Indicates that generated code should honor type-handlers registered at runtime.
    /// </summary>
    public UseRuntimeTypeHandlersAttribute(bool enabled = true) => _ = enabled;
}

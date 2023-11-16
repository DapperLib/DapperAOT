using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Dapper;

/// <summary>
/// Indicates the batch size to use when executing commands with a sequence of argument rows.
/// </summary>
[Conditional("DEBUG")] // not needed post-build, so: evaporate
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class BatchSizeAttribute : Attribute
{
    /// <summary>
    /// Indicates the batch size to use when executing commands with a sequence of argument row; a value of zero disables batch usage; a negative value uses a single batch for all rows.
    /// </summary>
    public BatchSizeAttribute(int batchSize) => _ = batchSize;
}

using System;
using System.ComponentModel;

namespace Dapper.CodeAnalysis
{
    /// <summary>
    /// Provides additional debug/test logging capabilities for source generators; not intended for public use - can change at any time.
    /// </summary>
    [Obsolete("Not intended for public usage.")]
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILoggingAnalyzer
    {
        /// <summary>
        /// Provide log feedback.
        /// </summary>
        event Action<string> Log;
    }
}

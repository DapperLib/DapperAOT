using System;
using System.ComponentModel;

namespace Dapper
{
    /// <summary>
    /// Allows fine-grained control over how single-row operations are handled
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [ImmutableObject(true)]
    public sealed class SingleRowAttribute : Attribute
    {
        /// <summary>
        /// Defines how single-row operations are handled
        /// </summary>
        public SingleRowKind Kind { get; }
        /// <summary>
        /// Create a new <see cref="SingleRowAttribute"/> instance
        /// </summary>
        /// <param name="kind"></param>
        public SingleRowAttribute(SingleRowKind kind = SingleRowKind.Automatic)
            => Kind = kind;
    }

    /// <summary>
    /// Allows fine-grained control over how single-row operations are handled
    /// </summary>
    public enum SingleRowKind
    {
        /// <summary>
        /// Infer safe behaviour from the context
        /// </summary>
        Automatic = -1,
        // note: the following *could* also be flags, "1=demand at least one row", "2=demand at most one row"
        /// <summary>
        /// Zero, one, or many rows are allowed; the first row is returned when present, otherwise the type's default is returned
        /// </summary>
        FirstOrDefault = 0,
        /// <summary>
        /// One or many rows are allowed; the first row is returned
        /// </summary>
        First = 1,
        /// <summary>
        /// Zero or one row is allowed; the first row is returned when present, otherwise the type's default is returned
        /// </summary>
        SingleOrDefault = 2,
        /// <summary>
        /// Exactly one rowis allowed and returned
        /// </summary>
        Single = 3,
        
    }
}

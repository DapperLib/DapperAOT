using System;
using System.ComponentModel;

namespace Dapper
{
    /// <summary>
    /// Indicates that a method represents a command, or that a parameter is the command text
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    [ImmutableObject(true)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// The SQL to use for this command.
        /// </summary>
        public string? CommandText { get; }

        /// <summary>
        /// Creates a new <see cref="CommandAttribute"/> instance with the specified <see cref="CommandText"/>.
        /// </summary>
        public CommandAttribute(string commandText)
            => CommandText = commandText;

        /// <summary>
        /// Creates a new <see cref="CommandAttribute"/> instance.
        /// </summary>
        public CommandAttribute() { }
    }
}

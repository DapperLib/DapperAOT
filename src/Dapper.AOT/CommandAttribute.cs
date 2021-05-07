using System;
using System.Data;
using System.Diagnostics;

namespace Dapper
{
    /// <summary>
    /// Indicates that a method represents a command, or that a parameter is the command text
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    [Conditional("DEBUG")]
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
        /// The <see cref="CommandType"/> to use for this operation (if omitted, uses <see cref="CommandType.Text"/> for commands with a space character, or <see cref="CommandType.StoredProcedure"/> otherwise).
        /// </summary>
        public CommandType CommandType { get; set; }

        /// <summary>
        /// Creates a new <see cref="CommandAttribute"/> instance.
        /// </summary>
        public CommandAttribute() { }
    }
}

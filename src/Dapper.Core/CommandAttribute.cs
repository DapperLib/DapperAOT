using System;

namespace Dapper
{
    /// <summary>
    /// Indicates that a method represents a command, or that a parameter is the command text
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class CommandAttribute : Attribute
    {
    }
}

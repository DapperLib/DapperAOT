using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Dapper;

/// <summary>
/// Describes a self-contained command, where there are either no parameters or the parameters are included in the setup.
/// </summary>
public readonly partial struct Command
{
    /// <summary>
    /// Create a command instance.
    /// </summary>
    public static Command Create(string sql, params ReadOnlySpan<Parameter> parameters)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// A parameter defined during SQL composition.
    /// </summary>
    public readonly struct Parameter(string name, object value, int size = 0)
        : IEquatable<Parameter>
    {
        /// <inheritdoc/>
        public override bool Equals(
#if NET6_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            object? obj)
            => obj is Parameter other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(Parameter other)
            => other.Name == this.Name
            && Equals(other.Value, this.Value)
            && other.Size == this.Size;

        /// <inheritdoc/>
        public override int GetHashCode() => Name is null ? 0 : Name.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"{Name}={Value}";

        /// <summary>
        /// The name of this parameter.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The value of this parameter.
        /// </summary>
        public object Value { get; } = value;

        /// <summary>
        /// The size of this parameter as defined by <see cref="DbParameter.Size"/>.
        /// </summary>
        public int Size { get; } = size;
    }
}

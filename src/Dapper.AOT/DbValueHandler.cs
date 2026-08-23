using System;
using System.ComponentModel;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Specify the handler type that should be used to read and write values of a given type; the
/// handler must implement <see cref="IDbValueHandler{T}"/>
/// (usually by inheriting <see cref="DbValueHandler{T}"/>), or be a vanilla Dapper
/// <c>SqlMapper.ITypeHandler</c>, in which case generated code adapts it.
/// </summary>
/// <remarks>
/// This is the replacement for runtime registration via <c>SqlMapper.AddTypeHandler</c>: it is
/// per-assembly rather than process-global, deterministic (no startup-ordering races), visible in
/// review, and known at compile time, so the generator can bake the dispatch.
/// <para>
/// Deliberately <b>not</b> generic, and deliberately not <c>[Conditional]</c>: generic attributes
/// cannot be read by .NET Framework's <c>GetCustomAttributes</c> (it throws for the whole call,
/// which would poison unrelated reflection over the assembly), and the metadata must survive the
/// build for a package to declare handlers for the types it owns.
/// </para>
/// </remarks>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct
    | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class TypeHandlerAttribute : Attribute
{
    /// <summary>
    /// Register <paramref name="handlerType"/> for all values of <paramref name="valueType"/>.
    /// </summary>
    public TypeHandlerAttribute(Type valueType, Type handlerType)
    {
        ValueType = valueType;
        HandlerType = handlerType;
    }

    /// <summary>
    /// Register <paramref name="handlerType"/> for the annotated member or parameter, inferring
    /// the value type from it.
    /// </summary>
    public TypeHandlerAttribute(Type handlerType) => HandlerType = handlerType;

    /// <summary>
    /// The type of value handled; <c>null</c> when inferred from the annotated member.
    /// </summary>
    public Type? ValueType { get; }

    /// <summary>
    /// The handler type; it must have a public parameterless constructor.
    /// </summary>
    public Type HandlerType { get; }
}

/// <summary>
/// Reads and writes values of type <typeparamref name="T"/>, replacing the conversion that
/// generated code would otherwise perform.
/// </summary>
/// <remarks>
/// Implement via <see cref="DbValueHandler{T}"/> unless you need to control every member. The
/// interface (rather than a base class) is the contract because generated code implements it to
/// adapt handlers from other libraries - notably vanilla Dapper's, which this library cannot
/// reference: a consumer may be using Dapper or Dapper.StrongName, and referencing either would
/// load both and split the registry.
/// </remarks>
public interface IDbValueHandler<T>
{
    /// <summary>Configure and assign a non-null value.</summary>
    void SetValue(DbParameter parameter, T value);

    /// <summary>Configure and assign a null value.</summary>
    void SetNullValue(DbParameter parameter);

    /// <summary>Interpret the value of an output parameter.</summary>
    T Parse(DbParameter parameter);

    /// <summary>
    /// Inspect a column once per query, returning a token that is passed to
    /// <see cref="Parse(DbDataReader, int, int)"/> for every row - so per-row type tests are paid
    /// once, matching how generated row factories work.
    /// </summary>
    int Tokenize(DbDataReader reader, int columnOffset);

    /// <summary>Read a value from a column, using the token from <see cref="Tokenize"/>.</summary>
    T Parse(DbDataReader reader, int ordinal, int token);
}

/// <summary>
/// Convenience base class for <see cref="IDbValueHandler{T}"/>; override what you need.
/// </summary>
public abstract class DbValueHandler<T> : IDbValueHandler<T>
{
    /// <summary>Configure a parameter (type, size, etc); applied for null and non-null alike.</summary>
    protected virtual void Configure(DbParameter parameter) { }

    /// <summary>Assign a non-null value, after <see cref="Configure"/>.</summary>
    protected abstract void SetValueCore(DbParameter parameter, T value);

    void IDbValueHandler<T>.SetValue(DbParameter parameter, T value)
    {
        Configure(parameter);
        SetValueCore(parameter, value);
    }

    void IDbValueHandler<T>.SetNullValue(DbParameter parameter)
    {
        Configure(parameter);
        parameter.Value = DBNull.Value;
    }

    /// <inheritdoc/>
    public virtual T Parse(DbParameter parameter) => Parse(parameter.Value);

    /// <inheritdoc/>
    public virtual int Tokenize(DbDataReader reader, int columnOffset) => 0;

    /// <inheritdoc/>
    public virtual T Parse(DbDataReader reader, int ordinal, int token) => Parse(reader.GetValue(ordinal));

    /// <summary>Interpret a raw value obtained from ADO.NET.</summary>
    protected abstract T Parse(object? value);
}

using Dapper.Internal;
using System;
using System.ComponentModel;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Specify that <typeparamref name="TTypeHandler"/> should be used as a custom type-handler
/// when processing values of type <typeparamref name="TValue"/>
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
public sealed class TypeHandlerAttribute<TValue, TTypeHandler> : Attribute
    where TTypeHandler : TypeHandler<TValue>, new()
{}

/// <summary>
/// Specify that <typeparamref name="TTypeHandler"/> should be used as a custom type-handler
/// when processing a specific type, member, or parameter
/// </summary>
[ImmutableObject(true)]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class TypeHandlerAttribute<TTypeHandler> : Attribute
    where TTypeHandler : class, new()
{ }

/// <summary>
/// Process a parameter value of type <typeparamref name="T"/>
/// </summary>
public abstract class TypeHandler<T>
{
    /// <summary>
    /// Configure a parameter
    /// </summary>
    protected virtual void Configure(DbParameter parameter) { }

    /// <summary>
    /// Assign a non-null value for a parameter
    /// </summary>
    protected virtual void SetValueCore(DbParameter parameter, T value)
        => parameter.Value = CommandFactory.AsGenericValue<T>(value);

    /// <summary>
    /// Configure and assign a non-null value for a parameter
    /// </summary>
    public void SetValue(DbParameter parameter, T value)
    {
        Configure(parameter);
        SetValueCore(parameter, value);
    }

    /// <summary>
    /// Configure and assign a null value for a parameter
    /// </summary>
    public void SetNullValue(DbParameter parameter)
    {
        Configure(parameter);
        parameter.Value = DBNull.Value;
    }

    /// <summary>
    /// Interpret the output value from a parameter
    /// </summary>
    public virtual T Parse(DbParameter parameter)
        => CommandUtils.As<T>(parameter.Value);

    /// <summary>
    /// Indicate whether a parameter should be treated as null
    /// </summary>
    public virtual bool IsDBNull(DbParameter parameter)
        => parameter.Value is null or DBNull;

    /// <summary>
    /// Flexibly interpret the value from a column
    /// </summary>
    public virtual T Parse(DbDataReader reader, int ordinal, int token)
        => CommandUtils.As<T>(reader.GetValue(ordinal));

    /// <summary>
    /// Get a token for how this column should be parsed
    /// </summary>
    public virtual int Tokenize(DbDataReader reader, int columnOffset) => 0;
}
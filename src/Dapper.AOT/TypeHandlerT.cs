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
[Obsolete("This registration was never implemented: nothing in the analyzer or generator has ever read it, so it has always been a no-op. Use the non-generic [TypeHandler(typeof(TValue), typeof(THandler))] with a handler implementing IDbValueHandler<TValue>.", error: false)]
public sealed class TypeHandlerAttribute<TValue, TTypeHandler> : Attribute
    where TTypeHandler : TypeHandler<TValue>, new()
{}

/// <summary>
/// Process a parameter value of type <typeparamref name="T"/>
/// </summary>
[Obsolete("This handler shape was never implemented: it exists only as the constraint of the obsolete generic [TypeHandler<,>] attribute, which was never read. Use IDbValueHandler<T> (or the DbValueHandler<T> base class), registered with [TypeHandler(typeof(T), typeof(THandler))].", error: false)]
public abstract class TypeHandler<T>
{
    /// <summary>
    /// Configure and assign a value for a parameter
    /// </summary>
    public virtual void SetValue(DbParameter parameter, T value)
        => parameter.Value = CommandFactory.AsGenericValue<T>(value);

    /// <summary>
    /// Interpret the output value from a parameter
    /// </summary>
    public virtual T Parse(DbParameter parameter)
        => CommandUtils.As<T>(parameter.Value);
}
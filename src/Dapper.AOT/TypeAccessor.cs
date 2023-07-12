using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dapper;

/// <summary>
/// Provides an API to access object properties via indexers.
/// </summary>
public static partial class TypeAccessor
{
    /// <summary>
    /// Create a <see cref="ObjectAccessor{T}"/> that accesses the specified typed instance.
    /// </summary>
    public static ObjectAccessor<T> CreateAccessor<T>(T obj, [DapperAot] TypeAccessor<T>? accessor = null)
    {
        if (accessor is null) ThrowNullAccessor();
        return new ObjectAccessor<T>(obj, accessor!);
    }

    static void ThrowNullAccessor() => throw new InvalidOperationException("The accessor was not provided (this is usually done via an 'interceptor' at build-time)");
}

/// <summary>
/// Provides an API to access object properties via indexers.
/// </summary>
public readonly struct ObjectAccessor<T> // this is mostly a placeholder for the *real* usage
{
    private readonly T obj;
    private readonly TypeAccessor<T> accessor;

    /// <summary>
    /// Create a new instance.
    /// </summary>
    public ObjectAccessor(T obj, TypeAccessor<T> accessor)
    {
        this.obj = obj;
        this.accessor = accessor;
    }

    /// <summary>
    /// Gets the number of members accessible via this instance.
    /// </summary>
    public int MemberCount => accessor.MemberCount;
    /// <summary>
    /// Gets or sets member values by index.
    /// </summary>
    public object? this[int index]
    {
        get => accessor[obj, index];
        set => accessor[obj, index] = value;
    }

    /// <summary>
    /// Gets or sets member values by name.
    /// </summary>
    public object? this[string key]
    {
        get => accessor[obj, key];
        set => accessor[obj, key] = value;
    }

    /// <summary>
    /// Gets a typed member value by index.
    /// </summary>
    public TValue GetValue<TValue>(int index) => accessor.GetValue<TValue>(obj, index);

    /// <summary>
    /// Sets a typed member value by index.
    /// </summary>
    public void SetValue<TValue>(int index, TValue value) => accessor.SetValue<TValue>(obj, index, value);

    /// <summary>
    /// Gets the type of the specified member.
    /// </summary>
    public Type GetType(int index) => accessor.GetType(index);

    /// <summary>
    /// Gets the name of the specified member.
    /// </summary>
    public string GetName(int index) => accessor.GetName(index);

    /// <summary>
    /// Indicates whether the specified member can be null.
    /// </summary>
    public bool IsNullable(int index) => accessor.IsNullable(index);
}

/// <summary>
/// Provides an API to access object properties via indexers.
/// </summary>
public abstract partial class TypeAccessor<T> // contains only members to intercept
{
    /// <summary>
    /// Attempt to identify the named member, using either exact or inexact (case-insensitive, ignores underscores) matching.
    /// </summary>
    public virtual int? TryIndex(string name, bool exact = false) => null;

    private int GetIndex(string name, bool exact = false)
    {
        var value = TryIndex(name, exact);
        return value.HasValue ? value.GetValueOrDefault() : KeyNotFound();

        static int KeyNotFound() => throw new KeyNotFoundException();
    }

    /// <summary>
    /// Indicates whether the specified member can be null.
    /// </summary>
    public virtual bool IsNullable(int index)
    {
        // default implementation: value-types that aren't Nullable<T>: false, otherwise true
        var type = GetType(index);
        return !(type.IsValueType && Nullable.GetUnderlyingType(type) is null);
    }

    /// <summary>
    /// Gets the number of members accessible via this instance.
    /// </summary>
    public virtual int MemberCount => 0;

    /// <summary>
    /// Gets the type of the specified member.
    /// </summary>
    public virtual Type GetType(int index) => throw new IndexOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the name of the specified member.
    /// </summary>
    public virtual string GetName(int index) => throw new IndexOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets or sets member values by index.
    /// </summary>
    public virtual object? this[T obj, int index]
    {
        get => throw new IndexOutOfRangeException(nameof(index));
        set => throw new IndexOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Indicates whether the specified value is null.
    /// </summary>
    public virtual bool IsNull(T obj, int index) => this[obj, index] is null or DBNull;

    /// <summary>
    /// Gets or sets member values by name.
    /// </summary>
    public virtual object? this[T obj, string key]
    {
        get => this[obj, GetIndex(key)];
        set => this[obj, GetIndex(key)] = value;
    }

    /// <summary>
    /// Gets a typed member value by index.
    /// </summary>
    public virtual TValue GetValue<TValue>(T obj, int index) => CommandUtils.As<TValue>(this[obj, index]);

    /// <summary>
    /// Sets a typed member value by index.
    /// </summary>
    public virtual void SetValue<TValue>(T obj, int index, TValue value) => this[obj, index] = value;

    /// <summary>
    /// Coerce a value-type from one type to another; this is primarily used to side-step generics, or to
    /// switch between enums and their underlying primitive value
    /// </summary>
    protected TTo UnsafePun<TFrom, TTo>(TFrom value)
    {
        if (typeof(TFrom) != typeof(TTo)) // if we're just side-stepping generics: they'll be the same
        {
            // try to check reasonableness
#if NETCOREAPP3_1_OR_GREATER
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<TFrom>());
        Debug.Assert(!RuntimeHelpers.IsReferenceOrContainsReferences<TTo>());
#else
            Debug.Assert(typeof(TFrom).IsValueType);
            Debug.Assert(typeof(TTo).IsValueType);
#endif
            Debug.Assert(Unsafe.SizeOf<TFrom>() == Unsafe.SizeOf<TTo>());
        }
        return Unsafe.As<TFrom, TTo>(ref value);
    }

    /// <summary>
    /// Provides a reliable string hashing function that ignores case, whitespace and underscores
    /// </summary>
    protected static uint NormalizedHash(string? value) => StringHashing.NormalizedHash(value);

    /// <summary>
    /// Compares a string for equality against a pre-normalized comparand, ignoring case, whitespace and underscores
    /// </summary>
    protected static bool NormalizedEquals(string? value, string? normalized) => StringHashing.NormalizedEquals(value, normalized);
}
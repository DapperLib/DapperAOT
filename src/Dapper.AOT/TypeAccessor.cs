using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Dapper;

public static class TypeAccessor
{
    public static ObjectAccessor<T> CreateAccessor<T>(T obj, [DapperAot] TypeAccessor<T>? accessor = null)
    {
        if (accessor is null) ThrowNull();
        return new ObjectAccessor<T>(obj, accessor!);
    }

    static void ThrowNull() => throw new InvalidOperationException("The accessor was not provided (this is usually done via an 'interceptor' at build-time)");
}

public readonly struct ObjectAccessor<T> // this is mostly a placeholder for the *real* usage
{
    private readonly T obj;
    private readonly TypeAccessor<T> accessor;

    public ObjectAccessor(T obj, TypeAccessor<T> accessor)
    {
        this.obj = obj;
        this.accessor = accessor;
    }

    public int MemberCount => accessor.MemberCount;
    public object? this[int index]
    {
        get => accessor[obj, index];
        set => accessor[obj, index] = value;
    }
    public object? this[string key]
    {
        get => accessor[obj, key];
        set => accessor[obj, key] = value;
    }
    public TValue GetValue<TValue>(int index) => accessor.GetValue<TValue>(obj, index);
    public void SetValue<TValue>(int index, TValue value) => accessor.SetValue<TValue>(obj, index, value);

    public Type GetType(int index) => accessor.GetType(index);
    public string GetName(int index) => accessor.GetName(index);
    public bool IsNullable(int index) => accessor.IsNullable(index);
}

public abstract partial class TypeAccessor<T> // contains only members to intercept
{
    public virtual int? TryIndex(string name, bool exact = false) => null;

    private int GetIndex(string name, bool exact = false)
    {
        var value = TryIndex(name, exact);
        return value.HasValue ? value.GetValueOrDefault() : KeyNotFound();

        static int KeyNotFound() => throw new KeyNotFoundException();
    }

    public virtual bool IsNullable(int index)
    {
        // default implementation: value-types that aren't Nullable<T>: false, otherwise true
        var type = GetType(index);
        return !(type.IsValueType && Nullable.GetUnderlyingType(type) is null);
    }
    public virtual int MemberCount => 0;

    public virtual Type GetType(int index) => throw new IndexOutOfRangeException(nameof(index));
    public virtual string GetName(int index) => throw new IndexOutOfRangeException(nameof(index));

    public virtual object? this[T obj, int index]
    {
        get => throw new IndexOutOfRangeException(nameof(index));
        set => throw new IndexOutOfRangeException(nameof(index));
    }
    public virtual object? this[T obj, string key]
    {
        get => this[obj, GetIndex(key)];
        set => this[obj, GetIndex(key)] = value;
    }

    public virtual TValue GetValue<TValue>(T obj, int index) => CommandUtils.As<TValue>(this[obj, index]);
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
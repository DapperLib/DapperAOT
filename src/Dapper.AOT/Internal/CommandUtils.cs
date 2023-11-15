using System;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dapper.Internal;

internal static class CommandUtils
{
#if NETCOREAPP3_1_OR_GREATER
    const MethodImplOptions AggressiveOptions = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
    const MethodImplOptions AggressiveOptions = MethodImplOptions.AggressiveInlining;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNone() => _ = System.Linq.Enumerable.First("");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMultiple() => _ = System.Linq.Enumerable.Single("  ");

    [MethodImpl(AggressiveOptions)]
    internal static ReadOnlySpan<int> UnsafeReadOnlySpan(int[] value, int length)
    {
        Debug.Assert(value is not null);
        Debug.Assert(length >= 0 && length <= value!.Length);
#if NET8_0_OR_GREATER
        return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(value), length);
#else
        return new ReadOnlySpan<int>(value, 0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static Span<int> UnsafeSlice(Span<int> value, int length)
    {
        Debug.Assert(length >= 0 && length <= value.Length);
#if NETCOREAPP3_1_OR_GREATER
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(value), length);
#else
        return value.Slice(0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static bool IsCompletedSuccessfully(this Task task)
    {
#if NETCOREAPP3_1_OR_GREATER
        return task.IsCompletedSuccessfully;
#else
        return task.Status == TaskStatus.RanToCompletion;
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static int CloseAndCapture(this DbDataReader? reader)
    {
        if (reader is null) return -1;
        var count = reader.RecordsAffected;
        reader.Close();
        return count;
    }

    [MethodImpl(AggressiveOptions)]
    internal static ValueTask<int> CloseAndCaptureAsync(this DbDataReader? reader)
    {
#if NETCOREAPP3_1_OR_GREATER
        if (reader is null) return new(-1);
        var count = reader.RecordsAffected;
        var pending = reader.CloseAsync();
        return pending.IsCompletedSuccessfully ? new(count) : Deferred(pending, count);
        static async ValueTask<int> Deferred(Task pending, int count)
        {
            await pending;
            return count;
        }
#else
        return new(CloseAndCapture(reader));
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNull() => throw new ArgumentNullException("value");

    [MethodImpl(AggressiveOptions)]
    internal static T As<T>(object? value)
    {
        if (value is null or DBNull)
        {
            // if value-typed and *not* Nullable<T>, then: that's an error
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null)
            {
                ThrowNull();
            }
            return default!;
        }
        else
        {
            if (value is T typed)
            {
                return typed;
            }
            string? s;
            // note we're using value-type T JIT dead-code removal to elide most of these checks
            if (typeof(T) == typeof(int))
            {
                int t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<int, T>(ref t);
            }
            if (typeof(T) == typeof(int?))
            {
                int? t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<int?, T>(ref t);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return Unsafe.As<bool, T>(ref t);
            }
            else if (typeof(T) == typeof(bool?))
            {
                bool? t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return Unsafe.As<bool?, T>(ref t);
            }
            else if (typeof(T) == typeof(float))
            {
                float t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return Unsafe.As<float, T>(ref t);
            }
            else if (typeof(T) == typeof(float?))
            {
                float? t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return Unsafe.As<float?, T>(ref t);
            }
            else if (typeof(T) == typeof(double))
            {
                double t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Unsafe.As<double, T>(ref t);
            }
            else if (typeof(T) == typeof(double?))
            {
                double? t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Unsafe.As<double?, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return Unsafe.As<decimal, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal? t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return Unsafe.As<decimal?, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                return Unsafe.As<DateTime, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                DateTime? t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                return Unsafe.As<DateTime?, T>(ref t);
            }
            else if (typeof(T) == typeof(Guid) && (s = value as string) is not null)
            {
                Guid t = Guid.Parse(s);
                return Unsafe.As<Guid, T>(ref t);
            }
            else if (typeof(T) == typeof(Guid?) && (s = value as string) is not null)
            {
                Guid? t = Guid.Parse(s);
                return Unsafe.As<Guid?, T>(ref t);
            }
            else if (typeof(T) == typeof(long))
            {
                long t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<long, T>(ref t);
            }
            else if (typeof(T) == typeof(long?))
            {
                long? t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<long?, T>(ref t);
            }
            else if (typeof(T) == typeof(short))
            {
                short t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<short, T>(ref t);
            }
            else if (typeof(T) == typeof(short?))
            {
                short? t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<short?, T>(ref t);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<sbyte, T>(ref t);
            }
            else if (typeof(T) == typeof(sbyte?))
            {
                sbyte? t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<sbyte?, T>(ref t);
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ulong, T>(ref t);
            }
            else if (typeof(T) == typeof(ulong?))
            {
                ulong? t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ulong?, T>(ref t);
            }
            else if (typeof(T) == typeof(uint))
            {
                uint t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<uint, T>(ref t);
            }
            else if (typeof(T) == typeof(uint?))
            {
                uint? t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<uint?, T>(ref t);
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ushort, T>(ref t);
            }
            else if (typeof(T) == typeof(ushort?))
            {
                ushort? t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ushort?, T>(ref t);
            }
            else if (typeof(T) == typeof(byte))
            {
                byte t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<byte, T>(ref t);
            }
            else if (typeof(T) == typeof(byte?))
            {
                byte? t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<byte?, T>(ref t);
            }
            else if (typeof(T) == typeof(char))
            {
                char t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                return Unsafe.As<char, T>(ref t);
            }
            else if (typeof(T) == typeof(char?))
            {
                char? t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                return Unsafe.As<char?, T>(ref t);
            }
            // this won't elide, but: we'll live with it
            else if (typeof(T) == typeof(string))
            {
                var t = Convert.ToString(value, CultureInfo.InvariantCulture)!;
                return Unsafe.As<string, T>(ref t);
            }
            else
            {
                return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
            }
        }
    }
}


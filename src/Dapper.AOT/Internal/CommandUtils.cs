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
#if NET6_0_OR_GREATER
            else if (typeof(T) == typeof(DateOnly))
            {
                if (value is DateOnly only) return Unsafe.As<DateOnly, T>(ref only);

                DateTime t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                var dateOnly = DateOnly.FromDateTime(t);
                return Unsafe.As<DateOnly, T>(ref dateOnly);
            }
            else if (typeof(T) == typeof(DateOnly?))
            {
                DateTime? t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                DateOnly? dateOnly = t is null ? null : DateOnly.FromDateTime(t.Value);
                return Unsafe.As<DateOnly?, T>(ref dateOnly);
            }
            else if (typeof(T) == typeof(TimeOnly))
            {
                if (value is TimeSpan timeSpan)
                {
                    var fromSpan = TimeOnly.FromTimeSpan(timeSpan);
                    return Unsafe.As<TimeOnly, T>(ref fromSpan);
                }

                DateTime t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                var timeOnly = TimeOnly.FromDateTime(t);
                return Unsafe.As<TimeOnly, T>(ref timeOnly);
            }
            else if (typeof(T) == typeof(TimeOnly?))
            {
                if (value is TimeSpan timeSpan)
                {
                    var fromSpan = TimeOnly.FromTimeSpan(timeSpan);
                    return Unsafe.As<TimeOnly, T>(ref fromSpan);
                }

                DateTime? t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                TimeOnly? timeOnly = t is null ? null : TimeOnly.FromDateTime(t.Value);
                return Unsafe.As<TimeOnly?, T>(ref timeOnly);
            }
#endif
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
                // Handle enum types: match original Dapper's enum conversion logic
                // Enums require special handling because:
                // 1. Databases may return different integer types (e.g., SQLite returns Int64 for INTEGER columns)
                // 2. The enum's underlying type might differ (e.g., enum with int underlying type vs Int64 from DB)
                // 3. Floating point values need conversion to the enum's underlying integral type first
                // Enum.ToObject handles the conversion from any integral type to the enum automatically
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                
                // Unwrap nullable first, like original Dapper does
                var effectiveType = underlyingType ?? targetType;
                
                if (effectiveType.IsEnum)
                {
                    // Special handling for float/double/decimal like original Dapper
                    if (value is float || value is double || value is decimal)
                    {
                        value = Convert.ChangeType(value, Enum.GetUnderlyingType(effectiveType), CultureInfo.InvariantCulture);
                    }
                    // Enum.ToObject returns the enum value boxed as object
                    // For nullable enums, the cast from object will fail, so we need to use Convert.ChangeType
                    // which properly handles boxing/unboxing for nullable types
                    return (T)Enum.ToObject(effectiveType, value);
                }
                else if (underlyingType is not null)
                {
                    // Other nullable types
                    return (T)Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                }
                else
                {
                    // Non-nullable, non-enum fallback
                    return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
            }
        }
    }
}


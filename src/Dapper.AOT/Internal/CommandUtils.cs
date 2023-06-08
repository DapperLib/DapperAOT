using System;
using System.Buffers;
using System.Data;
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

    /// <summary>
    /// Asserts that the connection provided is usable
    /// </summary>
    [MethodImpl(AggressiveOptions)]
    public static DbConnection TypeCheck(DbConnection? cnn)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(cnn);
#else
        if (cnn is null)
        {
            Throw();
        }
        static void Throw() => throw new ArgumentNullException(nameof(cnn));
#endif
        return cnn!;
    }

    /// <summary>
    /// Asserts that the connection provided is usable
    /// </summary>
    [MethodImpl(AggressiveOptions)]
    public static DbConnection TypeCheck(IDbConnection cnn)
    {
        if (cnn is not DbConnection typed)
        {
            Throw(cnn);
        }
        return typed;
        static void Throw(IDbConnection cnn)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(cnn);
#else
            if (cnn is null) throw new ArgumentNullException(nameof(cnn));
#endif
            throw new ArgumentException("The supplied connection must be a " + nameof(DbConnection), nameof(cnn));
        }
    }

    /// <summary>
    /// Asserts that the transaction provided is usable
    /// </summary>
    [MethodImpl(AggressiveOptions)]
    public static DbTransaction? TypeCheck(IDbTransaction? transaction)
    {
        if (transaction is null) return null;
        if (transaction is not DbTransaction typed)
        {
            Throw();
        }
        return typed;
        static void Throw() => throw new ArgumentException("The supplied transaction must be a " + nameof(DbTransaction), nameof(transaction));
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNone() => _ = System.Linq.Enumerable.First("");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMultiple() => _ = System.Linq.Enumerable.Single("  ");

    [MethodImpl(AggressiveOptions)]
    internal static ReadOnlySpan<int> UnsafeReadOnlySpan(int[] value, int length)
    {
        Debug.Assert(value is not null);
        Debug.Assert(length >= 0 && length <= value!.Length);
#if NET8_0_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(value), length);
#else
        return new ReadOnlySpan<int>(value, 0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static Span<int> UnsafeSlice(Span<int> value, int length)
    {
        Debug.Assert(length >= 0 && length <= value.Length);
#if NETCOREAPP3_1_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(value), length);
#else
        return value.Slice(0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static Span<int> UnsafeRent(out int[] leased, int length)
    {
        Debug.Assert(length >= 0);
        leased = ArrayPool<int>.Shared.Rent(length);
#if NET8_0_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(leased), length);
#else
        return new Span<int>(leased, 0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static void Return(int[]? leased)
    {
        if (leased is not null)
        {
            ArrayPool<int>.Shared.Return(leased);
        }
    }

    [MethodImpl(AggressiveOptions)]
    internal static void Cleanup(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        reader?.Dispose();
        command?.Dispose();
        if (closeConnection)
        {
            connection.Close();
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    internal static async ValueTask CleanupAsync(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        if (reader is not null)
        {
            await reader.DisposeAsync();
        }
        if (command is not null)
        {
            await command.DisposeAsync();
        }
        if (closeConnection)
        {
            await connection.CloseAsync();
        }
    }
#else
    [MethodImpl(AggressiveOptions)]
    internal static ValueTask CleanupAsync(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        Cleanup(reader, command, connection, closeConnection);
        return default;
    }
#endif

    [MethodImpl(AggressiveOptions)]
    internal static bool IsCompletedSuccessfully(this Task task)
    {
#if NETCOREAPP3_1_OR_GREATER
        return task.IsCompletedSuccessfully;
#else
        return task.Status == TaskStatus.RanToCompletion;
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


using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dapper.Internal;

#pragma warning disable CS1591 // intellisense


/// <summary>
/// Utilities to assist with interceptor AOT implementations
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
[Obsolete("Not intended for external use; this API can change at an time")]
public class InterceptorHelpers
{
    /// <summary>
    /// Used to reshape anonymous-type data into types that can be used statically
    /// </summary>
    public static TResult Reshape<TSource, TResult>(object? value, Func<TSource> _, Func<TSource, TResult> selector)
        => selector((TSource)value!);

    /// <summary>Identify column tokens from a reader</summary>
    public delegate void ColumnTokenizer(DbDataReader reader, Span<int> tokens, int columnOffset);
    /// <summary>Identify column tokens from a reader</summary>
    public delegate T RowParser<T>(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset);

    private const int MAX_STACK_TOKENS = 64;

    /// <summary>
    /// Perform a synchronous buffered query
    /// </summary>
    public static List<TResult> QueryBuffered<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        int[]? leased = null;
        CommandBehavior behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                closeConn = true;
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (timeout is not null)
            {
                cmd.CommandTimeout = timeout.GetValueOrDefault();
            }
            commandBuilder.Invoke(cmd, args);
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            var results = new List<TResult>();
            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                if (columnTokenizer is null | fieldCount == 0)
                {
                    if (rowReader is null)
                    {
                        do
                        {
                            results.Add(reader.GetFieldValue<TResult>(0));
                        }
                        while (reader.Read());
                    }
                    else
                    {
                        ReadOnlySpan<int> readOnlyTokens = default;
                        do
                        {
                            results.Add(rowReader(reader, readOnlyTokens, 0));
                        }
                        while (reader.Read());
                    }
                }
                else
                {
                    var readWriteTokens = fieldCount <= MAX_STACK_TOKENS ? stackalloc int[MAX_STACK_TOKENS] : (leased = ArrayPool<int>.Shared.Rent(fieldCount));
                    readWriteTokens = readWriteTokens.Slice(0, fieldCount);
                    columnTokenizer!(reader, readWriteTokens, 0);
                    ReadOnlySpan<int> readOnlyTokens = readWriteTokens;
                    do
                    {
                        results.Add(rowReader!(reader, readOnlyTokens, 0));
                    }
                    while (reader.Read());
                }
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            return default!;
        }
        finally
        {
            if (leased is not null)
            {
                ArrayPool<int>.Shared.Return(leased);
            }
            reader?.Dispose();
            cmd?.Dispose();
            if (closeConn)
            {
                connection.Close();
            }
        }
    }

    /// <summary>
    /// Perform a synchronous first-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QueryFirst<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, args, transaction, timeout, OneRowFlags.ThrowIfNone,
            commandBuilder, columnTokenizer, rowReader);

    /// <summary>
    /// Perform a synchronous first-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QueryFirstOrDefault<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, args, transaction, timeout, OneRowFlags.None,
            commandBuilder, columnTokenizer, rowReader);

    /// <summary>
    /// Perform a synchronous single-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QuerySingle<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, args, transaction, timeout, OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple,
            commandBuilder, columnTokenizer, rowReader);


    /// <summary>
    /// Perform a synchronous single-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QuerySingleOrDefault<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, args, transaction, timeout, OneRowFlags.ThrowIfMultiple,
            commandBuilder, columnTokenizer, rowReader);

    [Flags]
    private enum OneRowFlags
    {
        None = 0,
        ThrowIfNone = 1 << 0,
        ThrowIfMultiple = 1 << 1,
    }

    /// <summary>
    /// Perform a synchronous first-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static TResult UnsafeQueryFirst<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        delegate*<DbCommand, TArgs, void> commandBuilder,
        delegate*<DbDataReader, Span<int>, int, void> columnTokenizer,
        delegate*<DbDataReader, ReadOnlySpan<int>, int, TResult> rowReader)
        => UnsafeQueryOneRow(connection, sql, args, transaction, timeout, OneRowFlags.ThrowIfNone,
            commandBuilder, columnTokenizer, rowReader);

    private static TResult QueryOneRow<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        OneRowFlags oneRowFlags,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        int[]? leased = null;
        var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
        if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) == 0)
        {   // if we don't care if there's two rows, we can restrict to read one only
            behavior |= CommandBehavior.SingleRow;
        }

        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                closeConn = true;
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (timeout is not null)
            {
                cmd.CommandTimeout = timeout.GetValueOrDefault();
            }
            commandBuilder.Invoke(cmd, args);
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TResult result = default!;
            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                if (columnTokenizer is null | fieldCount == 0)
                {
                    if (rowReader is null)
                    {
                        result = reader.GetFieldValue<TResult>(0);
                    }
                    else
                    {
                        result = rowReader(reader, default, 0);
                    }
                }
                else
                {
                    var readWriteTokens = fieldCount <= MAX_STACK_TOKENS ? stackalloc int[MAX_STACK_TOKENS] : (leased = ArrayPool<int>.Shared.Rent(fieldCount));
                    readWriteTokens = readWriteTokens.Slice(0, fieldCount);
                    columnTokenizer!(reader, readWriteTokens, 0);
                    result = rowReader!(reader, readWriteTokens, 0);
                }

                if (reader.Read())
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        ThrowMultiple();
                    }
                    while (reader.Read()) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                ThrowNone();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            return result;
        }
        finally
        {
            if (leased is not null)
            {
                ArrayPool<int>.Shared.Return(leased);
            }
            reader?.Dispose();
            cmd?.Dispose();
            if (closeConn)
            {
                connection.Close();
            }
        }
    }

    private unsafe static TResult UnsafeQueryOneRow<TResult, TArgs>(
        DbConnection connection, string sql, TArgs args, DbTransaction? transaction, int? timeout,
        OneRowFlags oneRowFlags,
        delegate*<DbCommand, TArgs, void> commandBuilder,
        delegate*<DbDataReader, Span<int>, int, void> columnTokenizer,
        delegate*<DbDataReader, ReadOnlySpan<int>, int, TResult> rowReader)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        int[]? leased = null;
        CommandBehavior behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess | CommandBehavior.SingleRow;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                closeConn = true;
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (timeout is not null)
            {
                cmd.CommandTimeout = timeout.GetValueOrDefault();
            }
            commandBuilder(cmd, args);
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TResult result = default!;
            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                if (columnTokenizer is null | fieldCount == 0)
                {
                    if (rowReader is null)
                    {
                        result = reader.GetFieldValue<TResult>(0);
                    }
                    else
                    {
                        result = rowReader(reader, default, 0);
                    }
                }
                else
                {
                    var readWriteTokens = fieldCount <= MAX_STACK_TOKENS ? stackalloc int[MAX_STACK_TOKENS] : (leased = ArrayPool<int>.Shared.Rent(fieldCount));
                    readWriteTokens = readWriteTokens.Slice(0, fieldCount);
                    columnTokenizer(reader, readWriteTokens, 0);
                    result = rowReader(reader, readWriteTokens, 0);
                }

                if (reader.Read())
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
                    {
                        ThrowMultiple();
                    }
                    while (reader.Read()) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                ThrowNone();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            return result;
        }
        finally
        {
            if (leased is not null)
            {
                ArrayPool<int>.Shared.Return(leased);
            }
            reader?.Dispose();
            cmd?.Dispose();
            if (closeConn)
            {
                connection.Close();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNone() => _ = "".First();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMultiple() => _ = " ".Single();

    /*
    /// <summary>
    /// Perform a synchronous buffered query
    /// </summary>
    public static async Task<IEnumerable<TResult>> QueryBufferedAsync<TResult, TArgs>(
        DbConnection connection,
        DbTransaction? transaction,
        TArgs args, string sql, int? timeout, CommandBehavior behavior,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer columnTokenizer,
        RowParser<TResult> rowReader,
        CancellationToken cancellationToken)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        int[]? leased = null;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
                closeConn = true;
            }
            cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            cmd.CommandText = sql;
            if (timeout is not null)
            {
                cmd.CommandTimeout = timeout.GetValueOrDefault();
            }
            commandBuilder.Invoke(cmd, rawArgs is null ? default! : argsParser((TRawArgs)rawArgs));
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            var results = new List<TResult>();
            if (await reader.ReadAsync(cancellationToken))
            {
                var fieldCount = columnTokenizer is null ? 0 : reader.FieldCount;
                if (fieldCount != 0)
                {
                    leased = ArrayPool<int>.Shared.Rent(fieldCount);
                    columnTokenizer!(reader, new Span<int>(leased, 0, fieldCount), 0);
                }
                do
                {
#if NETCOREAPP3_1_OR_GREATER
                    results.Add(rowReader(reader, fieldCount == 0 ? default : MemoryMarshal.CreateReadOnlySpan(ref leased![0], fieldCount), 0));
#else
                    results.Add(rowReader(reader, new ReadOnlySpan<int>(leased, 0, fieldCount), 0));
#endif
                }
                while (await reader.ReadAsync(cancellationToken));
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            return default!;
        }
        finally
        {
            if (leased is not null)
            {
                ArrayPool<int>.Shared.Return(leased);
            }
#if NETCOREAPP3_1_OR_GREATER
            if (reader is not null)
            {
                await reader.DisposeAsync();
            }
            if (cmd is not null)
            {
                await cmd.DisposeAsync();
            }
            if (closeConn)
            {
                await connection.CloseAsync();
            }
#else
            reader?.Dispose();
            cmd?.Dispose();
            if (closeConn)
            {
                connection.Close();
            }
#endif
        }
    }
    */

    /// <summary>
    /// Asserts that the connection provided is usable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbConnection TypeCheck(DbConnection cnn)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbConnection TypeCheck(IDbConnection cnn)
    {
        if (cnn is not DbConnection typed)
        {
            Throw(cnn);
        }
        return typed;
        static void Throw(IDbConnection cnn)
        {
            if (cnn is null) throw new ArgumentNullException(nameof(cnn));
            throw new ArgumentException("The supplied connection must be a DbConnection", nameof(cnn));
        }
    }

    /// <summary>
    /// Asserts that the transaction provided is usable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbTransaction? TypeCheck(DbTransaction? transaction) => transaction;

    /// <summary>
    /// Asserts that the transaction provided is usable
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DbTransaction? TypeCheck(IDbTransaction? transaction)
    {
        if (transaction is null) return null;
        if (transaction is not DbTransaction typed)
        {
            Throw();
        }
        return typed;
        static void Throw() => throw new ArgumentException("The supplied transaction must be a DbTransaction", nameof(transaction));
    }

    public static T GetValue<T>(DbDataReader reader, int fieldOffset)
    {
        var value = reader.GetValue(fieldOffset);
        if (value is T typed)
        {
            // no conversion necessary
            return typed;
        }
        // note we're using value-type T JIT dead-code removal to elide most of these checks
        if (typeof(T) == typeof(int))
        {
            var t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return Unsafe.As<int, T>(ref t);
        }
        else if (typeof(T) == typeof(bool))
        {
            var t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            return Unsafe.As<bool, T>(ref t);
        }
        else if (typeof(T) == typeof(float))
        {
            var t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            return Unsafe.As<float, T>(ref t);
        }
        else if (typeof(T) == typeof(double))
        {
            var t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return Unsafe.As<double, T>(ref t);
        }
        else if (typeof(T) == typeof(decimal))
        {
            var t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return Unsafe.As<decimal, T>(ref t);
        }
        else if (typeof(T) == typeof(decimal))
        {
            var t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return Unsafe.As<decimal, T>(ref t);
        }
        else if (typeof(T) == typeof(DateTime))
        {
            var t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
            return Unsafe.As<DateTime, T>(ref t);
        }
        else if (typeof(T) == typeof(long))
        {
            var t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return Unsafe.As<long, T>(ref t);
        }
        else if (typeof(T) == typeof(short))
        {
            var t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
            return Unsafe.As<short, T>(ref t);
        }
        else if (typeof(T) == typeof(sbyte))
        {
            var t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
            return Unsafe.As<sbyte, T>(ref t);
        }
        else if (typeof(T) == typeof(ulong))
        {
            var t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
            return Unsafe.As<ulong, T>(ref t);
        }
        else if (typeof(T) == typeof(uint))
        {
            var t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            return Unsafe.As<uint, T>(ref t);
        }
        else if (typeof(T) == typeof(ushort))
        {
            var t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
            return Unsafe.As<ushort, T>(ref t);
        }
        else if (typeof(T) == typeof(byte))
        {
            var t = Convert.ToByte(value, CultureInfo.InvariantCulture);
            return Unsafe.As<byte, T>(ref t);
        }
        else if (typeof(T) == typeof(char))
        {
            var t = Convert.ToChar(value, CultureInfo.InvariantCulture);
            return Unsafe.As<char, T>(ref t);
        }
        // this won't elide, but: we'll live with it
        else if (typeof(T) == typeof(string))
        {
            var t = Convert.ToString(value, CultureInfo.InvariantCulture)!;
            return Unsafe.As<string, T>(ref t);
        }
        else
        {
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
    }

    private static readonly object[] s_BoxedInt32 = new object[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    private static readonly object s_BoxedTrue = true, s_BoxedFalse = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(int value)
        => value >= -1 && value <= 10 ? s_BoxedInt32[value + 1] : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(int? value)
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(bool value)
        => value ? s_BoxedTrue : s_BoxedFalse;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(bool? value)
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue<T>(T? value) where T : struct
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(object? value)
        => value ?? DBNull.Value;
}

/// <summary>
/// Helper APIs; not intended for public consumption
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
[Obsolete("Not intended for external use; this API can change at an time")]
public static partial class StringHashing
{ }

#pragma warning restore CS1591
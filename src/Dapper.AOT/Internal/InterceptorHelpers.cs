using System;
using System.Buffers;
using System.Collections.Generic;
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
[Obsolete("Not intended for external use; this API can change at an time")]
public class InterceptorHelpers
{
    /// <summary>Identify column tokens from a reader</summary>
    public delegate void ColumnTokenizer(DbDataReader reader, Span<int> tokens, int columnOffset);
    /// <summary>Identify column tokens from a reader</summary>
    public delegate T RowParser<T>(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset);

    private const int MAX_STACK_TOKENS = 64;

    /// <summary>
    /// Perform a synchronous buffered query
    /// </summary>
    public static List<TResult> QueryBuffered<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> unused,
        Func<TRawArgs?, TArgs> argsParser,
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
            commandBuilder.Invoke(cmd, argsParser((TRawArgs?)rawArgs));
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
    public static TResult QueryFirst<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> _,
        Func<TRawArgs?, TArgs> argsParser,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, rawArgs, transaction, timeout, OneRowFlags.ThrowIfNone,
            argsParser, commandBuilder, columnTokenizer, rowReader);

    /// <summary>
    /// Perform a synchronous first-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QueryFirstOrDefault<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> _,
        Func<TRawArgs?, TArgs> argsParser,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, rawArgs, transaction, timeout, OneRowFlags.None,
            argsParser, commandBuilder, columnTokenizer, rowReader);

    /// <summary>
    /// Perform a synchronous single-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QuerySingle<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> _,
        Func<TRawArgs?, TArgs> argsParser,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, rawArgs, transaction, timeout, OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple,
            argsParser, commandBuilder, columnTokenizer, rowReader);


    /// <summary>
    /// Perform a synchronous single-row query
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult QuerySingleOrDefault<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> _,
        Func<TRawArgs?, TArgs> argsParser,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer? columnTokenizer,
        RowParser<TResult>? rowReader)
        => QueryOneRow(connection, sql, rawArgs, transaction, timeout, OneRowFlags.ThrowIfMultiple,
            argsParser, commandBuilder, columnTokenizer, rowReader);

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
    public unsafe static TResult UnsafeQueryFirst<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        Func<TRawArgs> _,
        Func<TRawArgs?, TArgs> argsParser,
        delegate*<DbCommand, TArgs, void> commandBuilder,
        delegate*<DbDataReader, Span<int>, int, void> columnTokenizer,
        delegate*<DbDataReader, ReadOnlySpan<int>, int, TResult> rowReader)
        => UnsafeQueryOneRow(connection, sql, rawArgs, transaction, timeout, OneRowFlags.ThrowIfNone,
            argsParser, commandBuilder, columnTokenizer, rowReader);

    private static TResult QueryOneRow<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        OneRowFlags oneRowFlags,
        Func<TRawArgs?, TArgs> argsParser,
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
            commandBuilder.Invoke(cmd, argsParser((TRawArgs?)rawArgs));
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

    private unsafe static TResult UnsafeQueryOneRow<TResult, TRawArgs, TArgs>(
        DbConnection connection, string sql, object? rawArgs, DbTransaction? transaction, int? timeout,
        OneRowFlags oneRowFlags,
        Func<TRawArgs?, TArgs> argsParser,
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
            commandBuilder(cmd, argsParser((TRawArgs?)rawArgs));
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
    public static async Task<IEnumerable<TResult>> QueryBufferedAsync<TResult, TRawArgs, TArgs>(
        DbConnection connection,
        DbTransaction? transaction,
        object? rawArgs, string sql, int? timeout, CommandBehavior behavior,
        Func<TRawArgs> unused,
        Func<TRawArgs, TArgs> argsParser,
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

    public static int GetInt32(DbDataReader reader, int fieldOffset)
        => Convert.ToInt32(reader.GetValue(fieldOffset), CultureInfo.InvariantCulture);

    public static string GetString(DbDataReader reader, int fieldOffset)
        => Convert.ToString(reader.GetValue(fieldOffset), CultureInfo.InvariantCulture)!;

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
    // ... and a few others

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object AsValue(object? value)
        => value ?? DBNull.Value;
}

/// <summary>
/// Helper APIs; not intended for public consumption
/// </summary>
[Obsolete("Not intended for external use; this API can change at an time")]
public static partial class InternalUtilities
{ }

#pragma warning restore CS1591
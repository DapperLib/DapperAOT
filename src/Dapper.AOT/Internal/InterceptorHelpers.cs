using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal;




/// <summary>
/// Utilities to assist with interceptor AOT implementations
/// </summary>
[Obsolete("Not intended for external use; this API can change at an time")]
#if NET6_0_OR_GREATER
[System.Runtime.CompilerServices.SkipLocalsInit]
#endif
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
    public static List<TResult> QueryBuffered<TResult, TRawArgs, TArgs>(DbConnection connection,
        object? rawArgs, string sql, int? timeout, CommandBehavior behavior,
        Func<TRawArgs> unused,
        Func<TRawArgs?, TArgs> argsParser,
        Action<DbCommand, TArgs> commandBuilder,
        ColumnTokenizer columnTokenizer,
        RowParser<TResult> rowReader)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        int[]? leased = null;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                closeConn = true;
            }
            cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (timeout is not null)
            {
                cmd.CommandTimeout = timeout.GetValueOrDefault();
            }
            commandBuilder.Invoke(cmd, argsParser((TRawArgs?)rawArgs));
            reader = cmd.ExecuteReader(behavior);
            var results = new List<TResult>();
            if (reader.Read())
            {
                Span<int> readWriteTokens = reader.FieldCount <= MAX_STACK_TOKENS ? stackalloc int[MAX_STACK_TOKENS] : (leased = ArrayPool<int>.Shared.Rent(reader.FieldCount));
                readWriteTokens = readWriteTokens.Slice(0, reader.FieldCount);
                columnTokenizer(reader, readWriteTokens, 0);
                ReadOnlySpan<int> readOnlyTokens = readWriteTokens;
                do
                {
                    results.Add(rowReader(reader, readOnlyTokens, 0));
                }
                while (reader.Read());
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
                var fieldCount = reader.FieldCount;
                leased = ArrayPool<int>.Shared.Rent(fieldCount);
                columnTokenizer(reader, new Span<int>(leased, 0, fieldCount), 0);
                do
                {
#if NETCOREAPP3_1_OR_GREATER
                    results.Add(rowReader(reader, fieldCount == 0 ? default : MemoryMarshal.CreateReadOnlySpan(ref leased[0], fieldCount), 0));
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

    /// <summary>
    /// Asserts that the connection provided is usable
    /// </summary>
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
}

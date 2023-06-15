using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Represents the state required to perform an ADO.NET operation
/// </summary>
/// <typeparam name="TArgs">The type of data that holds the parameters for this command</typeparam>
#if !(NETSTANDARD || NETFRAMEWORK)
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "Equality not actually supported")]
#endif
public readonly struct Command<TArgs>
#if DEBUG
    : Dapper.Internal.ICommandApi
#endif
{
    private readonly CommandType commandType;
    private readonly int timeout;
    private readonly string sql;
    private readonly DbConnection connection;
    private readonly DbTransaction? transaction;
    private readonly CommandFactory<TArgs> commandFactory;
    private readonly TArgs args;

    /// <inheritdoc/>
    public override string ToString() => sql;

    /// <inheritdoc/>
    public override int GetHashCode()
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => throw new NotSupportedException();

    /// <summary>
    /// Allow this command to be referenced in a non-generic way
    /// </summary>
    public static implicit operator Command(Command<TArgs> value)
        => new Command.TypedCommand<TArgs>(value);

    /// <summary>
    /// Create a new instance
    /// </summary>
    internal Command(DbConnection? connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
    {
        if (connection is null)
        {
            if (transaction is null) ThrowNoConnection();
            connection = transaction!.Connection;
            if (connection is null) ThrowTransactionHasNoConnection();
        }
        else
        {
            if (transaction is not null && !ReferenceEquals(connection, transaction.Connection))
            {
                ThrowWrongTransactionConnection();
            }
        }

        this.connection = connection!;
        this.transaction = transaction;
        this.sql = sql;
        this.args = args;
        this.commandType = commandType;
        this.timeout = timeout;
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;

        static void ThrowTransactionHasNoConnection() => throw new ArgumentException("The transaction provided has no associated connection", nameof(transaction));
        static void ThrowWrongTransactionConnection() => throw new ArgumentException("The transaction provided is not associated with the specified connection", nameof(transaction));
        static void ThrowNoConnection() => throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Create a new instance
    /// </summary>
    internal Command(DbConnection connection, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
    {
        if (connection is null) ThrowNoConnection();

        this.connection = connection!;
        this.transaction = null;
        this.sql = sql;
        this.args = args;
        this.commandType = commandType;
        this.timeout = timeout;
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;

        static void ThrowNoConnection() => throw new ArgumentNullException(nameof(connection));
    }

    internal void PostProcessAndRecycle(ref CommandState state)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(ref QueryState state)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(ref CommandState state, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal DbCommand GetCommand()
    {
        var cmd = commandFactory.GetCommand(connection!, sql, commandType, args);
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        if (timeout >= 0)
        {
            cmd.CommandTimeout = timeout;
        }
        return cmd;
    }

    /// <summary>
    /// Reads exactly one row
    /// </summary>
    public TRow QuerySingle<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory)!;

    /// <summary>
    /// Reads the first row returned
    /// </summary>
    public TRow QueryFirst<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfNone, rowFactory)!;

    /// <summary>
    /// Reads at most one row
    /// </summary>
    public TRow? QuerySingleOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfMultiple, rowFactory);

    /// <summary>
    /// Reads the first row, if any are returned
    /// </summary>
    public TRow? QueryFirstOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.None, rowFactory);

    /// <summary>
    /// Reads exactly one row
    /// </summary>
    public Task<TRow> QuerySingleAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken)!;

    /// <summary>
    /// Reads the first row returned
    /// </summary>
    public Task<TRow> QueryFirstAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfNone, rowFactory, cancellationToken)!;

    /// <summary>
    /// Reads at most one row
    /// </summary>
    public Task<TRow?> QuerySingleOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken);

    /// <summary>
    /// Reads the first row, if any are returned
    /// </summary>
    public Task<TRow?> QueryFirstOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.None, rowFactory, cancellationToken);

    /// <summary>
    /// Executes a non-query operation
    /// </summary>
    public int Execute()
    {
        CommandState state = default;
        try
        {
            var result = state.ExecuteNonQuery(GetCommand());
            PostProcessAndRecycle(ref state, result);
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Executes a non-query operation
    /// </summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        CommandState state = default;
        try
        {
            var result = await state.ExecuteNonQueryAsync(GetCommand(), cancellationToken);
            PostProcessAndRecycle(ref state, result);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public object? ExecuteScalar()
    {
        CommandState state = default;
        try
        {
            var result = state.ExecuteScalar(GetCommand());
            PostProcessAndRecycle(ref state);
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        CommandState state = default;
        try
        {
            var result = await state.ExecuteScalarAsync(GetCommand(), cancellationToken);
            PostProcessAndRecycle(ref state);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public T ExecuteScalar<T>()
    {
        CommandState state = default;
        try
        {
            var result = state.ExecuteScalar(GetCommand());
            PostProcessAndRecycle(ref state);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default)
    {
        CommandState state = default;
        try
        {
            var result = await state.ExecuteScalarAsync(GetCommand(), cancellationToken);
            PostProcessAndRecycle(ref state);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads rows from a query, optionally buffered
    /// </summary>
    public IEnumerable<TRow> Query<TRow>(bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => buffered ? QueryBuffered(rowFactory) : QueryUnbuffered(rowFactory);

    /// <summary>
    /// Reads buffered rows from a query
    /// </summary>
    public List<TRow> QueryBuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        QueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            var results = new List<TRow>();
            if (state.Reader.Read())
            {
                var readWriteTokens = state.Reader.FieldCount <= MAX_STACK_TOKENS
                    ? CommandUtils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], state.Reader.FieldCount)
                    : state.Lease();

                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, readWriteTokens, 0);
                ReadOnlySpan<int> readOnlyTokens = readWriteTokens; // avoid multiple conversions
                do
                {
                    results.Add(rowFactory.Read(state.Reader, readOnlyTokens, 0));
                }
                while (state.Reader.Read());
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state);
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a query
    /// </summary>
    public async Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
    {
        QueryState state = default;
        try
        {
            await state.ExecuteReaderAsync(GetCommand(), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            var results = new List<TRow>();
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                do
                {
                    results.Add(rowFactory.Read(state.Reader, state.Tokens, 0));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(ref state);
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a query
    /// </summary>
    public async IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        QueryState state = default;
        try
        {
            await state.ExecuteReaderAsync(GetCommand(), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                do
                {
                    yield return rowFactory.Read(state.Reader, state.Tokens, 0);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(ref state);
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a query
    /// </summary>
    public IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        QueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                do
                {
                    yield return rowFactory.Read(state.Reader, state.Tokens, 0);
                }
                while (state.Reader.Read());
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state);
        }
        finally
        {
            state.Dispose();
        }
    }

    const int MAX_STACK_TOKENS = 64;

    // if we don't care if there's two rows, we can restrict to read one only
    static CommandBehavior SingleFlags(OneRowFlags flags)
        => (flags & OneRowFlags.ThrowIfMultiple) == 0
            ? CommandBehavior.SingleResult | CommandBehavior.SequentialAccess | CommandBehavior.SingleRow
            : CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    private TRow? QueryOneRow<TRow>(
        OneRowFlags flags,
        RowFactory<TRow>? rowFactory)
    {
        QueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(), SingleFlags(flags));

            TRow? result = default;
            if (state.Reader.Read())
            {
                var readWriteTokens = state.Reader.FieldCount <= MAX_STACK_TOKENS
                    ? CommandUtils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], state.Reader.FieldCount)
                    : state.Lease();

                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, readWriteTokens, 0);
                result = rowFactory.Read(state.Reader, readWriteTokens, 0);
                state.Return();

                if (state.Reader.Read())
                {
                    if ((flags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        CommandUtils.ThrowMultiple();
                    }
                    while (state.Reader.Read()) { }
                }
            }
            else if ((flags & OneRowFlags.ThrowIfNone) != 0)
            {
                CommandUtils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state);
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    private async Task<TRow?> QueryOneRowAsync<TRow>(
        OneRowFlags flags,
        RowFactory<TRow>? rowFactory,
        CancellationToken cancellationToken)
    {
        QueryState state = default;

        try
        {
            await state.ExecuteReaderAsync(GetCommand(), SingleFlags(flags), cancellationToken);

            TRow? result = default;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);

                result = rowFactory.Read(state.Reader, state.Tokens, 0);
                state.Return();

                if (await state.Reader.ReadAsync(cancellationToken))
                {
                    if ((flags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        CommandUtils.ThrowMultiple();
                    }
                    while (await state.Reader.ReadAsync(cancellationToken)) { }
                }
            }
            else if ((flags & OneRowFlags.ThrowIfNone) != 0)
            {
                CommandUtils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(ref state);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}

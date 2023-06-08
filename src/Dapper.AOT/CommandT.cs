using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Represents the state required to perform an ADO.NET operation
/// </summary>
/// <typeparam name="TArgs">The type of data that holds the parameters for this command</typeparam>
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

    /// <summary>
    /// Allow this command to be referenced in a non-generic way
    /// </summary>
    public static implicit operator Command(Command<TArgs> value)
        => new Command.TypedCommand<TArgs>(value);

    /// <summary>
    /// Create a new instance
    /// </summary>
    public Command(IDbConnection connection, IDbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
        : this(CommandUtils.TypeCheck(connection), CommandUtils.TypeCheck(transaction), sql, args, commandType, timeout, commandFactory) {}

    /// <summary>
    /// Create a new instance
    /// </summary>
    public Command(DbConnection connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
    {
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(connection);
#else
        if (connection is null) throw new ArgumentNullException(nameof(connection));
#endif
        this.connection = connection;
        this.transaction = transaction;
        this.sql = sql;
        this.args = args;
        this.commandType = commandType;
        this.timeout = timeout;
    }

    private void PostProcessCommand(ref DbCommand? command)
    {
        Debug.Assert(command is not null);
        if (commandFactory.PostProcess(command!, args))
        {
            command = null;
        }
    }

    private DbCommand GetCommand()
    {
        var cmd = commandFactory.Prepare(connection!, sql, commandType, args);
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        if (timeout >= 0)
        {
            cmd.CommandTimeout = timeout;
        }
        return cmd;
    }

    private bool OpenIfNeeded()
    {
        if (connection.State == ConnectionState.Open)
        {
            return false;
        }
        else
        {
            connection.Open();
            return true;
        }
    }

    private ValueTask<bool> OpenIfNeededAsync(CancellationToken cancellationToken)
    {
        if (connection.State == ConnectionState.Open)
        {
            return default;
        }
        else
        {
            var pending = connection.OpenAsync(cancellationToken);
            return pending.IsCompletedSuccessfully()
                ? new(true) : Awaited(pending);

            static async ValueTask<bool> Awaited(Task pending)
            {
                await pending;
                return true;
            }
        }
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
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = OpenIfNeeded();
            cmd = GetCommand();
            var result = cmd.ExecuteNonQuery();
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            CommandUtils.Cleanup(null, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Executes a non-query operation
    /// </summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = await OpenIfNeededAsync(cancellationToken);
            cmd = GetCommand();
            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            await CommandUtils.CleanupAsync(null, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public object? ExecuteScalar()
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = OpenIfNeeded();
            cmd = GetCommand();
            var result = cmd.ExecuteScalar();
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            CommandUtils.Cleanup(null, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = await OpenIfNeededAsync(cancellationToken);
            cmd = GetCommand();
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            await CommandUtils.CleanupAsync(null, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public T ExecuteScalar<T>()
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = OpenIfNeeded();
            cmd = GetCommand();
            var result = cmd.ExecuteScalar();
            PostProcessCommand(ref cmd);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            CommandUtils.Cleanup(null, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = await OpenIfNeededAsync(cancellationToken);
            cmd = GetCommand();
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            PostProcessCommand(ref cmd);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            await CommandUtils.CleanupAsync(null, cmd, connection, closeConn);
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
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            var results = new List<TRow>();
            if (reader.Read())
            {
                int[]? leased = null;
                var fieldCount = reader.FieldCount;
                var readWriteTokens
                    = fieldCount == 0 ? default
                    : fieldCount <= MAX_STACK_TOKENS ? CommandUtils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], fieldCount)
                    : CommandUtils.UnsafeRent(out leased, fieldCount);

                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, readWriteTokens, 0);
                ReadOnlySpan<int> readOnlyTokens = readWriteTokens; // avoid multiple conversions
                do
                {
                    results.Add(rowFactory.Read(reader, readOnlyTokens, 0));
                }
                while (reader.Read());
                CommandUtils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
            return results;
        }
        finally
        {
            CommandUtils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Reads buffered rows from a query
    /// </summary>
    public async Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            var results = new List<TRow>();
            if (await reader.ReadAsync(cancellationToken))
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, CommandUtils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    results.Add(rowFactory.Read(reader, CommandUtils.UnsafeReadOnlySpan(leased, fieldCount), 0));
                }
                while (await reader.ReadAsync(cancellationToken));
                CommandUtils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
            return results;
        }
        finally
        {
            await CommandUtils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a query
    /// </summary>
    public async IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            if (await reader.ReadAsync(cancellationToken))
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, CommandUtils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    yield return rowFactory.Read(reader, CommandUtils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                }
                while (await reader.ReadAsync(cancellationToken));
                CommandUtils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
        }
        finally
        {
            await CommandUtils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a query
    /// </summary>
    public IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, CommandUtils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    yield return rowFactory.Read(reader, CommandUtils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                }
                while (reader.Read());
                CommandUtils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
        }
        finally
        {
            CommandUtils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    const int MAX_STACK_TOKENS = 64;

    private TRow? QueryOneRow<TRow>(
        OneRowFlags oneRowFlags,
        RowFactory<TRow>? rowFactory)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;

        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) == 0)
            {   // if we don't care if there's two rows, we can restrict to read one only
                behavior |= CommandBehavior.SingleRow;
            }
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TRow? result = default;
            if (reader.Read())
            {
                {   // extra scope level so the compiler can ensure we aren't using the lease beyond the expected lifetime
                    int[]? leased = null;
                    var fieldCount = reader.FieldCount;
                    var readWriteTokens
                        = fieldCount == 0 ? default
                        : fieldCount <= MAX_STACK_TOKENS ? CommandUtils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], fieldCount)
                        : CommandUtils.UnsafeRent(out leased, fieldCount);

                    (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, readWriteTokens, 0);
                    result = rowFactory.Read(reader, readWriteTokens, 0);
                    CommandUtils.Return(leased);
                }

                if (reader.Read())
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        CommandUtils.ThrowMultiple();
                    }
                    while (reader.Read()) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                CommandUtils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            CommandUtils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    private async Task<TRow?> QueryOneRowAsync<TRow>(
        OneRowFlags oneRowFlags,
        RowFactory<TRow>? rowFactory,
        CancellationToken cancellationToken)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;

        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) == 0)
            {   // if we don't care if there's two rows, we can restrict to read one only
                behavior |= CommandBehavior.SingleRow;
            }
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TRow? result = default;
            if (await reader.ReadAsync(cancellationToken))
            {
                {   // extra scope level so the compiler can ensure we aren't using the lease beyond the expected lifetime
                    var fieldCount = reader.FieldCount;
                    (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, CommandUtils.UnsafeRent(out var leased, fieldCount), 0);

                    result = rowFactory.Read(reader, CommandUtils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                    CommandUtils.Return(leased);
                }

                if (await reader.ReadAsync(cancellationToken))
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        CommandUtils.ThrowMultiple();
                    }
                    while (await reader.ReadAsync(cancellationToken)) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                CommandUtils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            await CommandUtils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }
}

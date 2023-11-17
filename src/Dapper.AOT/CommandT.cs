using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Represents a command that will be executed for a series of <typeparamref name="TArgs"/> values
/// </summary>
#pragma warning disable IDE0079 // unnecessary suppression; it is necessary on some TFMs
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "Equality not actually supported")]
#pragma warning restore IDE0079
public readonly partial struct Command<TArgs> : ICommand<TArgs>
{
    private readonly CommandType commandType;
    private readonly int timeout;
    private readonly string sql;
    private readonly DbConnection connection;
    private readonly DbTransaction? transaction;
    private readonly CommandFactory<TArgs> commandFactory;

    /// <summary>
    /// Create a new instance
    /// </summary>
    internal Command(DbConnection connection, string sql, CommandType commandType, int timeout,
        [DapperAot] CommandFactory<TArgs>? commandFactory = null)
    {
        if (connection is null) ThrowNoConnection();

        this.connection = connection!;
        this.transaction = null;
        this.sql = sql;
        this.commandType = commandType;
        this.timeout = timeout;
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;

        static void ThrowNoConnection() => throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>
    /// Create a new instance
    /// </summary>
    internal Command(DbConnection? connection, DbTransaction? transaction, string sql, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
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
        this.commandType = commandType == 0 ? DapperAotExtensions.GetCommandType(sql) : commandType;
        this.timeout = timeout;
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;

        static void ThrowTransactionHasNoConnection() => throw new ArgumentException("The transaction provided has no associated connection", nameof(transaction));
        static void ThrowWrongTransactionConnection() => throw new ArgumentException("The transaction provided is not associated with the specified connection", nameof(transaction));
        static void ThrowNoConnection() => throw new ArgumentNullException(nameof(connection));
    }

    private DbCommand GetCommand(TArgs args)
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

    private void GetUnifiedCommand(out UnifiedCommand command, TArgs args)
    {
#if NET6_0_OR_GREATER
        if (connection.CanCreateBatch && commandFactory.CanExpandIntoBatch(sql))
        {
            var batch = connection.CreateBatch();
            batch.Connection = connection;
            batch.Transaction = transaction;
            if (timeout >= 0)
            {
                batch.Timeout = timeout;
            }
            command = new UnifiedCommand(batch);
            commandFactory.ExpandIntoBatch(in command, sql, args);
        }
        else
#endif
        {
            var cmd = GetCommand(args);
            command = new UnifiedCommand(cmd);
        }
    }

    internal void PostProcessAndRecycle(ref SyncQueryState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(new(state.Command!), args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycleUnified(ref SyncQueryState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is null); // all on unified command now
#if NET6_0_OR_GREATER
        var batch = state.CommandState.UnifiedCommand.Batch;
        if (batch is not null)
        {
            // deliberately do not expose the outer batch to the caller
            commandFactory.PostProcessBatch(batch.BatchCommands, args, rowCount, 0);
        }
        else
#endif
        {
            commandFactory.PostProcess(in state.CommandState.UnifiedCommand, args, rowCount);
        }
        if (state.CommandState.UnifiedCommand.Command is { } cmd && commandFactory.TryRecycle(cmd))
        {
            state.CommandState.UnifiedCommand.UnsafeSetCommand(null);
        }
    }

    internal void PostProcessAndRecycle(AsyncQueryState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(new(state.Command!), args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(ref SyncCommandState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(new(state.Command!), args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(AsyncCommandState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(new(state.Command!), args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    /// <summary>
    /// Read the data as a <see cref="DbDataReader"/>
    /// </summary>
    public DbDataReader ExecuteReader(TArgs args, CommandBehavior behavior = CommandBehavior.Default)
        => ExecuteReader<WrappedDbDataReader>(args, behavior);

    /// <summary>
    /// Read the data as a <see cref="DbDataReader"/>
    /// </summary>
    public DbDataReader ExecuteReader<TReader>(TArgs args, CommandBehavior behavior = CommandBehavior.Default)
        where TReader : WrappedDbDataReader, new()
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), behavior);
            var obj = new TReader();
            obj.Initialize(commandFactory, args, ref state);
            return obj;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Read the data as a <see cref="DbDataReader"/>
    /// </summary>
    public Task<DbDataReader> ExecuteReaderAsync(TArgs args, CommandBehavior behavior = CommandBehavior.Default, CancellationToken cancellationToken = default)
        => ExecuteReaderAsync<WrappedDbDataReader>(args, behavior, cancellationToken);

    /// <summary>
    /// Read the data as a <see cref="DbDataReader"/>
    /// </summary>
    public async Task<DbDataReader> ExecuteReaderAsync<TReader>(TArgs args, CommandBehavior behavior = CommandBehavior.Default, CancellationToken cancellationToken = default)
        where TReader : WrappedDbDataReader, new()
    {
        AsyncQueryState? state = new();
        try
        {
            await state.ExecuteReaderAsync(GetCommand(args), behavior, cancellationToken);
            var obj = new TReader();
            obj.Initialize(commandFactory, args, ref state);
            return obj;
        }
        finally
        {
            if (state is not null)
            {
                await state.DisposeAsync();
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() => sql;

    /// <inheritdoc/>
    public override int GetHashCode()
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => throw new NotSupportedException();
}
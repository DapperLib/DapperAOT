using Dapper.Internal;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

    internal void PostProcessAndRecycle(ref CommandState state, TArgs args)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(ref QueryState state, TArgs args)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void PostProcessAndRecycle(ref CommandState state, TArgs args, int rowCount)
    {
        Debug.Assert(state.Command is not null);
        commandFactory.PostProcess(state.Command!, args, rowCount);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    /// <summary>
    /// Read the data as a <see cref="DbDataReader"/>
    /// </summary>
    public DbDataReader ExecuteReader(TArgs args, CommandBehavior behavior = CommandBehavior.Default)
    {
        QueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), behavior);
            return new WrappedReader<TArgs>(commandFactory, args, ref state);
        }
        finally
        {
            state.Dispose();
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
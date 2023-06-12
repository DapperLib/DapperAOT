using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Represents a command that will be executed for a series of <typeparamref name="TArgs"/> values
/// </summary>
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "Equality not actually supported")]
public readonly struct Batch<TArgs>
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
    internal Batch(DbConnection connection, string sql, CommandType commandType, int timeout,
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
    internal Batch(DbConnection? connection, DbTransaction? transaction, string sql, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
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
        this.commandType = commandType;
        this.timeout = timeout;
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;

        static void ThrowTransactionHasNoConnection() => throw new ArgumentException("The transaction provided has no associated connection", nameof(transaction));
        static void ThrowWrongTransactionConnection() => throw new ArgumentException("The transaction provided is not associated with the specified connection", nameof(transaction));
        static void ThrowNoConnection() => throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc/>
    public override string ToString() => sql;

    /// <inheritdoc/>
    public override int GetHashCode()
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => throw new NotSupportedException();

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(ReadOnlySpan<TArgs> values)
        => values.IsEmpty ? 0 : Execute(new BatchSyncIterator<TArgs>(values));

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(IEnumerable<TArgs> values)
    {
        return values switch
        {
            null => 0,
#if NET6_0_OR_GREATER
            List<TArgs> list => Execute(CollectionsMarshal.AsSpan(list)),
#endif
            TArgs[] arr => arr.Length == 0 ? 0 : Execute(new ReadOnlySpan<TArgs>(arr)),
            ArraySegment<TArgs> segment => segment.Count == 0 ? 0 : Execute(new ReadOnlySpan<TArgs>(segment.Array!, segment.Offset, segment.Count)),
            ICollection<TArgs> collection when collection.Count is 0 => 0,
            IReadOnlyCollection<TArgs> collection when collection.Count is 0 => 0,
            _ => Execute(new BatchSyncIterator<TArgs>(values)),
        };
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

    internal void Recycle(ref CommandState state)
    {
        Debug.Assert(state.Command is not null);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    private int Execute(BatchSyncIterator<TArgs> source)
    {
        CommandState state = default;
        try
        {
            int total = 0;
            if (source.MoveNext(out var value))
            {
                bool haveMore = source.MoveNext(out value);
                if (haveMore) state.PrepareBeforeExecute();
                var local = state.ExecuteNonQuery(GetCommand(value));
                commandFactory.PostProcess(state.Command, value, local);
                total += local;

                while (haveMore)
                {
                    commandFactory.UpdateParameters(state.Command, value);
                    local = state.ExecuteNonQuery(state.Command);
                    commandFactory.PostProcess(state.Command, value, local);
                    total += local;

                    haveMore = source.MoveNext(out value);
                }
                Recycle(ref state);
                return total;
            }
            return total;
        }
        finally
        {
            source.Dispose();
            state.Dispose();
        }
    }

    //public Task<int> ExecuteAsync(ReadOnlyMemory<TArgs> values, CancellationToken cancellationToken = default)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<int> ExecuteAsync(IEnumerable<TArgs> values, CancellationToken cancellationToken = default)
    //{
    //    throw new NotImplementedException();
    //}

    //public Task<int> ExecuteAsync(IAsyncEnumerable<TArgs> values, CancellationToken cancellationToken = default)
    //{
    //    throw new NotImplementedException();
    //}

}
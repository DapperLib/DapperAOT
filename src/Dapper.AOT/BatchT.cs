using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
#if !NETSTANDARD
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "Equality not actually supported")]
#endif
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
    public int Execute(ReadOnlySpan<TArgs> values) => values.Length switch
    {
        0 => 0,
        1 => Execute(values[0]),
        _ => ExecuteMulti(values),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(List<TArgs> values)
    {
        if (values is null) return 0;
        return values.Count switch
        {
            0 => 0,
            1 => Execute(values[0]),
#if NET6_0_OR_GREATER
            _ => ExecuteMulti(CollectionsMarshal.AsSpan(values)),
#else
            _ => ExecuteMulti(values),
#endif
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(IEnumerable<TArgs> values) => values switch
    {
        null => 0,
        List<TArgs> list => Execute(list),
        TArgs[] arr => Execute(arr),
        ArraySegment<TArgs> segment => Execute(new ReadOnlySpan<TArgs>(segment.Array!, segment.Offset, segment.Count)),
        ImmutableArray<TArgs> arr => Execute(arr),
        ICollection<TArgs> collection when collection.Count is 0 => 0, // note that IList<T> : ICollection<T>
        IList<TArgs> collection when collection.Count is 1 => Execute(collection[0]),
        IReadOnlyCollection<TArgs> collection when collection.Count is 0 => 0, // note that IReadOnlyList<T> : IReadOnlyCollection<T>
        IReadOnlyList<TArgs> collection when collection.Count is 1 => Execute(collection[0]),
        _ => ExecuteMulti(values),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(TArgs[] values)
    {
        if (values is null) return 0;
        return values.Length switch
        {
            0 => 0,
            1 => Execute(values[0]),
            _ => ExecuteMulti(new ReadOnlySpan<TArgs>(values)),
        };
    }


    static void Validate(TArgs[] values, int offset, int count)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset + count, values is null ? 0 : values.Length, nameof(count));
#else
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (count < 0 || (offset + count) > (values is null ? 0 : values.Length)) throw new ArgumentOutOfRangeException(nameof(count));
#endif
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(TArgs[] values, int offset, int count)
    {
        Validate(values, offset, count);
        return count switch
        {
            0 => 0,
            1 => Execute(values[offset]),
            _ => ExecuteMulti(new ReadOnlySpan<TArgs>(values, offset, count)),
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(TArgs[] values, CancellationToken cancellationToken = default)
    {
        if (values is null) return TaskZero;
        return values.Length switch
        {
            0 => TaskZero,
            1 => ExecuteAsync(values[0], cancellationToken),
            _ => ExecuteMultiAsync(values, 0, values.Length, cancellationToken),
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(List<TArgs> values, CancellationToken cancellationToken = default)
    {
        if (values is null) return TaskZero;
        return values.Count switch
        {
            0 => TaskZero,
            1 => ExecuteAsync(values[0], cancellationToken),
            _ => ExecuteMultiAsync(values, cancellationToken),
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(TArgs[] values, int offset, int count, CancellationToken cancellationToken = default)
    {
        Validate(values, offset, count);
        return count switch
        {
            0 => TaskZero,
            1 => ExecuteAsync(values[offset], cancellationToken),
            _ => ExecuteMultiAsync(values, offset, count, cancellationToken),
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

    private int Execute(TArgs value)
        => new Command<TArgs>(connection, transaction, sql, value, commandType, timeout, commandFactory).Execute();

    private Task<int> ExecuteAsync(TArgs value, CancellationToken cancellationToken)
    => new Command<TArgs>(connection, transaction, sql, value, commandType, timeout, commandFactory).ExecuteAsync(cancellationToken);

    private int ExecuteMulti(ReadOnlySpan<TArgs> source)
    {
        Debug.Assert(source.Length > 1);
        CommandState state = default;
        try
        {
            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source[0];

            var local = state.ExecuteNonQuery(GetCommand(current));
            commandFactory.PostProcess(state.Command, current, local);
            total += local;

            for (int i = 1; i < source.Length;i++)
            {
                current = source[i];
                commandFactory.UpdateParameters(state.Command, current);
                local = state.Command.ExecuteNonQuery();
                commandFactory.PostProcess(state.Command, current, local);
                total += local;
            }

            Recycle(ref state);
            return total;
        }
        finally
        {
            state.Dispose();
        }
    }

    private int ExecuteMulti(IEnumerable<TArgs> source)
    {
        CommandState state = default;
        var iterator = source.GetEnumerator();
        try
        {
            int total = 0;
            if (iterator.MoveNext())
            {
                var current = iterator.Current;
                bool haveMore = iterator.MoveNext();
                if (haveMore && commandFactory.CanPrepare) state.PrepareBeforeExecute();
                var local = state.ExecuteNonQuery(GetCommand(current));
                commandFactory.PostProcess(state.Command, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(state.Command, current);
                    local = state.Command.ExecuteNonQuery();
                    commandFactory.PostProcess(state.Command, current, local);
                    total += local;

                    haveMore = iterator.MoveNext();
                }
                Recycle(ref state);
                return total;
            }
            return total;
        }
        finally
        {
            iterator.Dispose();
            state.Dispose();
        }
    }

    private static readonly Task<int> TaskZero = Task.FromResult(0);

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(ReadOnlyMemory<TArgs> values, CancellationToken cancellationToken = default) => values.Length switch
    {
        0 => TaskZero,
        1 => ExecuteAsync(values.Span[0], cancellationToken),
        _ => MemoryMarshal.TryGetArray(values, out var segment) ? ExecuteMultiAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken) : ExecuteMultiAsync(values, cancellationToken),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(IEnumerable<TArgs> values, CancellationToken cancellationToken = default) => values switch
    {
        null => TaskZero,
        List<TArgs> list => ExecuteAsync(list, cancellationToken),
        TArgs[] arr => ExecuteAsync(arr, cancellationToken),
        ArraySegment<TArgs> segment => ExecuteMultiAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken),
        ImmutableArray<TArgs> arr => ExecuteAsync(arr, cancellationToken),
        ICollection<TArgs> collection when collection.Count is 0 => TaskZero, // note that IList<T> : ICollection<T>
        IList<TArgs> collection when collection.Count is 1 => ExecuteAsync(collection[0], cancellationToken),
        IReadOnlyCollection<TArgs> collection when collection.Count is 0 => TaskZero, // note that IReadOnlyList<T> : IReadOnlyCollection<T>
        IReadOnlyList<TArgs> collection when collection.Count is 1 => ExecuteAsync(collection[0], cancellationToken),
        _ => ExecuteMultiAsync(values, cancellationToken),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(IAsyncEnumerable<TArgs> values, CancellationToken cancellationToken = default)
        => values is null ? TaskZero : ExecuteMultiAsync(values, cancellationToken);

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(ImmutableArray<TArgs> values)
    {
        if (values.IsDefaultOrEmpty) return 0;

        return values.Length switch
        {
            1 => Execute(values[0]),
            _ => ExecuteMulti(values.AsSpan()),
        };
    }
    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(ImmutableArray<TArgs> values, CancellationToken cancellationToken = default)
    {
        if (values.IsDefaultOrEmpty) return TaskZero;
        
        return values.Length switch
        {
            1 => ExecuteAsync(values[0], cancellationToken),
            _ => ExecuteMultiAsync(values.AsMemory(), cancellationToken),
        };
    }

    private async Task<int> ExecuteMultiAsync(ReadOnlyMemory<TArgs> source, CancellationToken cancellationToken)
    {
        Debug.Assert(source.Length > 1);
        CommandState state = default;
        try
        {
            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source.Span[0];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
            commandFactory.PostProcess(state.Command, current, local);
            total += local;

            for (int i = 1; i < source.Length; i++)
            {
                current = source.Span[i];
                commandFactory.UpdateParameters(state.Command, current);
                local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                commandFactory.PostProcess(state.Command, current, local);
                total += local;
            }

            Recycle(ref state);
            return total;
        }
        finally
        {
            state.Dispose();
        }
    }

    private async Task<int> ExecuteMultiAsync(IAsyncEnumerable<TArgs> source, CancellationToken cancellationToken)
    {
        CommandState state = default;
        var iterator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            int total = 0;
            if (await iterator.MoveNextAsync())
            {
                var current = iterator.Current;
                bool haveMore = await iterator.MoveNextAsync();
                if (haveMore && commandFactory.CanPrepare) state.PrepareBeforeExecute();
                var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
                commandFactory.PostProcess(state.Command, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(state.Command, current);
                    local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                    commandFactory.PostProcess(state.Command, current, local);
                    total += local;

                    haveMore = await iterator.MoveNextAsync();
                }
                Recycle(ref state);
                return total;
            }
            return total;
        }
        finally
        {
            await iterator.DisposeAsync();
            await state.DisposeAsync();
        }
    }

    private async Task<int> ExecuteMultiAsync(IEnumerable<TArgs> source, CancellationToken cancellationToken)
    {
        CommandState state = default;
        var iterator = source.GetEnumerator();
        try
        {
            int total = 0;
            if (iterator.MoveNext())
            {
                var current = iterator.Current;
                bool haveMore = iterator.MoveNext();
                if (haveMore && commandFactory.CanPrepare) state.PrepareBeforeExecute();
                var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
                commandFactory.PostProcess(state.Command, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(state.Command, current);
                    local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                    commandFactory.PostProcess(state.Command, current, local);
                    total += local;

                    haveMore = iterator.MoveNext();
                }
                Recycle(ref state);
                return total;
            }
            return total;
        }
        finally
        {
            iterator.Dispose();
            await state.DisposeAsync();
        }
    }

    private async Task<int> ExecuteMultiAsync(TArgs[] source, int offset, int count, CancellationToken cancellationToken)
    {
        CommandState state = default;
        try
        {
            // count is now actually "end"
            count += offset;

            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source[offset++];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
            commandFactory.PostProcess(state.Command, current, local);
            total += local;

            while (offset < count) // actually "offset < end"
            {
                current = source[offset++];
                commandFactory.UpdateParameters(state.Command, current);
                local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                commandFactory.PostProcess(state.Command, current, local);
                total += local;
            }

            Recycle(ref state);
            return total;
        }
        finally
        {
            state.Dispose();
        }
    }

}
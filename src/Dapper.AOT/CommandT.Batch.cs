using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

partial struct Command<TArgs>
{
    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(ReadOnlySpan<TArgs> values, int batchSize = -1) => values.Length switch
    {
        0 => 0,
        1 => Execute(values[0]),
        _ => ExecuteMulti(values, batchSize),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(List<TArgs> values, int batchSize = -1)
    {
        if (values is null) return 0;
        return values.Count switch
        {
            0 => 0,
            1 => Execute(values[0]),
#if NET6_0_OR_GREATER
            _ => ExecuteMulti(CollectionsMarshal.AsSpan(values), batchSize),
#else
            _ => ExecuteMulti(values, batchSize),
#endif
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(IEnumerable<TArgs> values, int batchSize = -1) => values switch
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
        _ => ExecuteMulti(values, batchSize),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(TArgs[] values, int batchSize = -1)
    {
        if (values is null) return 0;
        return values.Length switch
        {
            0 => 0,
            1 => Execute(values[0]),
            _ => ExecuteMulti(new ReadOnlySpan<TArgs>(values), batchSize),
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
    public int Execute(TArgs[] values, int offset, int count, int batchSize = -1)
    {
        Validate(values, offset, count);
        return count switch
        {
            0 => 0,
            1 => Execute(values[offset]),
            _ => ExecuteMulti(new ReadOnlySpan<TArgs>(values, offset, count), batchSize),
        };
    }

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(TArgs[] values, int batchSize = -1, CancellationToken cancellationToken = default)
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
    public Task<int> ExecuteAsync(List<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default)
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
    public Task<int> ExecuteAsync(TArgs[] values, int offset, int count, int batchSize = -1, CancellationToken cancellationToken = default)
    {
        Validate(values, offset, count);
        return count switch
        {
            0 => TaskZero,
            1 => ExecuteAsync(values[offset], cancellationToken),
            _ => ExecuteMultiAsync(values, offset, count, cancellationToken),
        };
    }

    internal void Recycle(ref SyncCommandState state)
    {
        Debug.Assert(state.Command is not null);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    internal void Recycle(AsyncCommandState state)
    {
        Debug.Assert(state.Command is not null);
        if (commandFactory.TryRecycle(state.Command!))
        {
            state.Command = null;
        }
    }

    private int ExecuteMulti(ReadOnlySpan<TArgs> source, int batchSize)
    {
#if NET6_0_OR_GREATER
        if (UseBatch) return ExecuteMultiBatch(source, batchSize);
#endif
        return ExecuteMultiSequential(source);
    }

    private int ExecuteMultiSequential(ReadOnlySpan<TArgs> source)
    {
        Debug.Assert(source.Length > 1);
        SyncCommandState state = default;
        try
        {
            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source[0];

            var local = state.ExecuteNonQuery(GetCommand(current));
            commandFactory.PostProcess(state.Command, current, local);
            total += local;

            for (int i = 1; i < source.Length; i++)
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

#if NET6_0_OR_GREATER
    private bool UseBatch => connection is { CanCreateBatch: true } && commandFactory is { SupportBatch: true };
    private DbBatchCommand AddCommand(ref DbBatch? batch, TArgs args)
    {
        if (batch is null)
        {
            batch = connection.CreateBatch();
            batch.Connection = connection;
        }
        var cmd = commandFactory.GetCommand(batch, sql, commandType, args);
        batch.BatchCommands.Add(cmd);
        return cmd;
    }
    private int ExecuteMultiBatch(ReadOnlySpan<TArgs> source, int batchSize)
    {
        Debug.Assert(source.Length > 1);
        DbBatch? batch = null;
        try
        {
            foreach (var arg in source)
            {
                AddCommand(ref batch, arg);
            }
            if (batch is null) return 0;

            var result = batch.ExecuteNonQuery();

            if (commandFactory.RequirePostProcess)
            {
                int i = 0;
                foreach (var arg in source)
                {
                    var cmd = batch.BatchCommands[i++];
                    commandFactory.PostProcess(cmd, arg, cmd.RecordsAffected);
                }
            }
            return result;
        }
        finally
        {
            batch?.Dispose();
        }
    }
    private int ExecuteMultiBatch(IEnumerable<TArgs> source, int batchSize)
    {
        if (commandFactory.RequirePostProcess)
        {
            // try to ensure it is repeatable
            source = (source as IReadOnlyCollection<TArgs>) ?? source.ToList();
        }
        
        DbBatch? batch = null;
        try
        {
            foreach (var arg in source)
            {
                AddCommand(ref batch, arg);
            }
            if (batch is null) return 0;

            var result = batch.ExecuteNonQuery();
            if (commandFactory.RequirePostProcess)
            {
                int i = 0;
                foreach (var arg in source)
                {
                    var cmd = batch.BatchCommands[i++];
                    commandFactory.PostProcess(cmd, arg, cmd.RecordsAffected);
                }
            }
            return result;
        }
        finally
        {
            batch?.Dispose();
        }
    }
#endif

    private int ExecuteMulti(IEnumerable<TArgs> source, int batchSize)
    {
#if NET6_0_OR_GREATER
        if (UseBatch) return ExecuteMultiBatch(source, batchSize);
#endif
        return ExecuteMultiSequential(source);
    }
    
    private int ExecuteMultiSequential(IEnumerable<TArgs> source)
    {
        SyncCommandState state = default;
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
    public Task<int> ExecuteAsync(ReadOnlyMemory<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default) => values.Length switch
    {
        0 => TaskZero,
        1 => ExecuteAsync(values.Span[0], cancellationToken),
        _ => MemoryMarshal.TryGetArray(values, out var segment) ? ExecuteMultiAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken) : ExecuteMultiAsync(values, cancellationToken),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(IEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default) => values switch
    {
        null => TaskZero,
        List<TArgs> list => ExecuteAsync(list, batchSize, cancellationToken),
        TArgs[] arr => ExecuteAsync(arr, batchSize, cancellationToken),
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
    public Task<int> ExecuteAsync(IAsyncEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default)
        => values is null ? TaskZero : ExecuteMultiAsync(values, cancellationToken);

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public int Execute(ImmutableArray<TArgs> values, int batchSize = -1)
    {
        if (values.IsDefaultOrEmpty) return 0;

        return values.Length switch
        {
            1 => Execute(values[0]),
            _ => ExecuteMulti(values.AsSpan(), batchSize),
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
        AsyncCommandState state = new();
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

            Recycle(state);
            return total;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    private async Task<int> ExecuteMultiAsync(IAsyncEnumerable<TArgs> source, CancellationToken cancellationToken)
    {
        AsyncCommandState state = new();
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
                Recycle(state);
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
        AsyncCommandState state = new();
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
                Recycle(state);
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
        AsyncCommandState state = new();
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

            Recycle(state);
            return total;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}

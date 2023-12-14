using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            _ => ExecuteMultiAsync(values, 0, values.Length, batchSize, cancellationToken),
        };
    }

    // if not specified on the API, fetch cancellation from the command-factory
    private CancellationToken GetCancellationToken(TArgs args, CancellationToken cancellationToken)
        => cancellationToken.CanBeCanceled ? cancellationToken : commandFactory.GetCancellationToken(args);

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
            _ => ExecuteMultiAsync(values, batchSize, cancellationToken),
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
            _ => ExecuteMultiAsync(values, offset, count, batchSize, cancellationToken),
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ExecuteMulti(ReadOnlySpan<TArgs> source, int batchSize)
    {
#if NET6_0_OR_GREATER
        if (UseBatch(batchSize)) return ExecuteMultiBatch(source, batchSize);
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
            UnifiedCommand cmdState = new(state.Command);
            commandFactory.PostProcess(in cmdState, current, local);
            total += local;

            for (int i = 1; i < source.Length; i++)
            {
                current = source[i];
                commandFactory.UpdateParameters(cmdState, current);
                local = state.Command.ExecuteNonQuery();
                commandFactory.PostProcess(cmdState, current, local);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool UseBatch(int batchSize) => batchSize != 0 && connection is { CanCreateBatch: true };

    private DbBatchCommand AddCommand(ref UnifiedCommand state, TArgs args)
    {
        var cmd = state.UnsafeCreateNewCommand();
        commandFactory.Initialize(state, sql, commandType, args);
        state.AssertBatchCommands.Add(cmd);
        return cmd;
    }

    private int ExecuteMultiBatch(ReadOnlySpan<TArgs> source, int batchSize) // TODO: sub-batching
    {
        Debug.Assert(source.Length > 1);
        UnifiedCommand batch = default;
        try
        {
            foreach (var arg in source)
            {
                if (!batch.HasBatch) batch = new(connection.CreateBatch());
                AddCommand(ref batch, arg);
            }
            if (!batch.HasBatch) return 0;

            var result = batch.AssertBatch.ExecuteNonQuery();

            if (commandFactory.RequirePostProcess)
            {
                batch.PostProcess(source, commandFactory);
            }
            return result;
        }
        finally
        {
            batch.Cleanup();
        }
    }
    private int ExecuteMultiBatch(IEnumerable<TArgs> source, int batchSize) // TODO: sub-batching
    {
        if (commandFactory.RequirePostProcess)
        {
            // try to ensure it is repeatable
            source = (source as IReadOnlyCollection<TArgs>) ?? source.ToList();
        }

        UnifiedCommand batch = default;
        try
        {
            foreach (var arg in source)
            {
                if (!batch.HasBatch) batch = new(connection.CreateBatch());
                AddCommand(ref batch, arg);
            }
            if (!batch.HasBatch) return 0;

            var result = batch.AssertBatch.ExecuteNonQuery();
            if (commandFactory.RequirePostProcess)
            {
                batch.PostProcess(source, commandFactory);
            }
            return result;
        }
        finally
        {
            batch.Cleanup();
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ExecuteMulti(IEnumerable<TArgs> source, int batchSize)
    {
#if NET6_0_OR_GREATER
        if (UseBatch(batchSize)) return ExecuteMultiBatch(source, batchSize);
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
                UnifiedCommand cmdState = new(state.Command);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(in cmdState, current);
                    local = state.Command.ExecuteNonQuery();
                    commandFactory.PostProcess(in cmdState, current, local);
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
        _ => MemoryMarshal.TryGetArray(values, out var segment) ? ExecuteMultiAsync(segment.Array!, segment.Offset, segment.Count, batchSize, cancellationToken) : ExecuteMultiAsync(values, batchSize, cancellationToken),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(IEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default) => values switch
    {
        null => TaskZero,
        List<TArgs> list => ExecuteAsync(list, batchSize, cancellationToken),
        TArgs[] arr => ExecuteAsync(arr, batchSize, cancellationToken),
        ArraySegment<TArgs> segment => ExecuteMultiAsync(segment.Array!, segment.Offset, segment.Count, batchSize, cancellationToken),
        ImmutableArray<TArgs> arr => ExecuteAsync(arr, batchSize, cancellationToken),
        ICollection<TArgs> collection when collection.Count is 0 => TaskZero, // note that IList<T> : ICollection<T>
        IList<TArgs> collection when collection.Count is 1 => ExecuteAsync(collection[0], cancellationToken),
        IReadOnlyCollection<TArgs> collection when collection.Count is 0 => TaskZero, // note that IReadOnlyList<T> : IReadOnlyCollection<T>
        IReadOnlyList<TArgs> collection when collection.Count is 1 => ExecuteAsync(collection[0], cancellationToken),
        _ => ExecuteMultiAsync(values, batchSize, cancellationToken),
    };

    /// <summary>
    /// Execute an operation against a batch of inputs, returning the sum of all results
    /// </summary>
    public Task<int> ExecuteAsync(IAsyncEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default)
        => values is null ? TaskZero : ExecuteMultiAsync(values, batchSize, cancellationToken);

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
    public Task<int> ExecuteAsync(ImmutableArray<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default)
    {
        if (values.IsDefaultOrEmpty) return TaskZero;

        return values.Length switch
        {
            1 => ExecuteAsync(values[0], cancellationToken),
            _ => ExecuteMultiAsync(values.AsMemory(), batchSize, cancellationToken),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<int> ExecuteMultiAsync(ReadOnlyMemory<TArgs> source, int batchSize, CancellationToken cancellationToken)
    {
//#if NET6_0_OR_GREATER
//        if (UseBatch(batchSize)) return ExecuteMultiBatchAsync(source, batchSize, cancellationToken);
//#endif
        return ExecuteMultiSequentialAsync(source, cancellationToken);
    }

    private async Task<int> ExecuteMultiSequentialAsync(ReadOnlyMemory<TArgs> source, CancellationToken cancellationToken)
    {
        Debug.Assert(source.Length > 1);
        AsyncCommandState state = new();
        try
        {
            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source.Span[0];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), GetCancellationToken(current, cancellationToken));
            UnifiedCommand cmdState = new(state.Command);
            commandFactory.PostProcess(in cmdState, current, local);
            total += local;

            for (int i = 1; i < source.Length; i++)
            {
                current = source.Span[i];
                commandFactory.UpdateParameters(in cmdState, current);
                local = await state.Command.ExecuteNonQueryAsync(GetCancellationToken(current, cancellationToken));
                commandFactory.PostProcess(in cmdState, current, local);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<int> ExecuteMultiAsync(IAsyncEnumerable<TArgs> source, int batchSize, CancellationToken cancellationToken)
    {
//#if NET6_0_OR_GREATER
//        if (UseBatch(batchSize)) return ExecuteMultiBatchAsync(source, batchSize, cancellationToken);
//#endif
        return ExecuteMultiSequentialAsync(source, cancellationToken);
    }
    private async Task<int> ExecuteMultiSequentialAsync(IAsyncEnumerable<TArgs> source, CancellationToken cancellationToken)
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
                var local = await state.ExecuteNonQueryAsync(GetCommand(current), GetCancellationToken(current, cancellationToken));
                UnifiedCommand cmdState = new(state.Command);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(in cmdState, current);
                    local = await state.Command.ExecuteNonQueryAsync(GetCancellationToken(current, cancellationToken));
                    commandFactory.PostProcess(in cmdState, current, local);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<int> ExecuteMultiAsync(IEnumerable<TArgs> source, int batchSize, CancellationToken cancellationToken)
    {
//#if NET6_0_OR_GREATER
//        if (UseBatch(batchSize)) return ExecuteMultiBatchAsync(source, batchSize, cancellationToken);
//#endif
        return ExecuteMultiSequentialAsync(source, cancellationToken);
    }

    private async Task<int> ExecuteMultiSequentialAsync(IEnumerable<TArgs> source, CancellationToken cancellationToken)
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
                var local = await state.ExecuteNonQueryAsync(GetCommand(current), GetCancellationToken(current, cancellationToken));
                UnifiedCommand cmdState = new(state.Command);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(in cmdState, current);
                    local = await state.Command.ExecuteNonQueryAsync(GetCancellationToken(current, cancellationToken));
                    commandFactory.PostProcess(in cmdState, current, local);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<int> ExecuteMultiAsync(TArgs[] source, int offset, int count, int batchSize, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        if (UseBatch(batchSize)) return ExecuteMultiBatchAsync(source, offset, count, batchSize, cancellationToken);
#endif
        return ExecuteMultiSequentialAsync(source, offset, count, cancellationToken);
    }
    private async Task<int> ExecuteMultiSequentialAsync(TArgs[] source, int offset, int count, CancellationToken cancellationToken)
    {
        AsyncCommandState state = new();
        try
        {
            // count is now actually "end"
            count += offset;

            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source[offset++];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), GetCancellationToken(current, cancellationToken));
            UnifiedCommand cmdState = new(state.Command);
            commandFactory.PostProcess(in cmdState, current, local);
            total += local;

            while (offset < count) // actually "offset < end"
            {
                current = source[offset++];
                commandFactory.UpdateParameters(in cmdState, current);
                local = await state.Command.ExecuteNonQueryAsync(GetCancellationToken(current, cancellationToken));
                commandFactory.PostProcess(in cmdState, current, local);
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

#if NET6_0_OR_GREATER
    private async Task<int> ExecuteMultiBatchAsync(TArgs[] source, int offset, int count, int batchSize, CancellationToken cancellationToken) // TODO: sub-batching
    {
        Debug.Assert(source.Length > 1);
        UnifiedCommand batch = default;
        var end = offset + count;
        try
        {
            for (int i = offset ; i < end; i++)
            {
                if (!batch.HasBatch) batch = new(connection.CreateBatch());
                AddCommand(ref batch, source[i]);
            }
            if (!batch.HasBatch) return 0;

            var result = await batch.AssertBatch.ExecuteNonQueryAsync(cancellationToken);

            if (commandFactory.RequirePostProcess)
            {
                batch.PostProcess(new ReadOnlySpan<TArgs>(source, offset, count), commandFactory);
            }
            return result;
        }
        finally
        {
            batch.Cleanup();
        }
    }
#endif
}

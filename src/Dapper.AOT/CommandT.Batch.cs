using Dapper.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            UnifiedCommand cmdState = new(commandFactory, state.Command);
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

            state.UnifiedBatch.TryRecycle();
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

    void Add(ref UnifiedBatch batch, TArgs args)
    {
        if (batch.IsDefault)
        {
            // create a new batch initialized on a ready command
            batch = new(commandFactory, connection, transaction);
            if (timeout >= 0) batch.TimeoutSeconds = timeout;
            Debug.Assert(batch.Mode is BatchMode.MultiCommandDbBatchCommand); // current expectations
            batch.SetCommand(sql, commandType);
            commandFactory.AddParameters(in batch.Command, args);
        }
        else if (batch.IsLastCommand)
        {
            // create a new command at the end of the batch
            batch.CreateNextBatchGroup(sql, commandType);
            commandFactory.AddParameters(in batch.Command, args);
        }
        else
        {
            // overwriting
            batch.OverwriteNextBatchGroup();
            commandFactory.UpdateParameters(in batch.Command, args);
        }
    }

    private int ExecuteMultiBatch(ReadOnlySpan<TArgs> source, int batchSize)
    {
        Debug.Assert(source.Length > 1);
        SyncCommandState state = default;
        // note that we currently only use single-command-per-TArg mode, i.e. UseBatch is ignored
        try
        {
            int sum = 0, ppOffset = 0;
            foreach (var arg in source)
            {
                Add(ref state.UnifiedBatch, arg);
                if (state.UnifiedBatch.GroupCount == batchSize)
                {
                    sum += state.ExecuteNonQueryUnified();
                    PostProcessMultiBatch(in state.UnifiedBatch, ref ppOffset, source);
                }
            }
            if (state.UnifiedBatch.GroupCount != 0)
            {
                state.UnifiedBatch.Trim();
                sum += state.ExecuteNonQueryUnified();
                PostProcessMultiBatch(in state.UnifiedBatch, ref ppOffset, source);
            }
            state.UnifiedBatch.TryRecycle();
            return sum;
        }
        finally
        {
            state.Dispose();
        }
    }

    private void PostProcessMultiBatch(in UnifiedBatch batch, ReadOnlySpan<TArgs> args)
    {
        int i = 0;
        PostProcessMultiBatch(batch, ref i, args);
    }

    private void PostProcessMultiBatch(in UnifiedBatch batch, ref int argOffset, ReadOnlySpan<TArgs> args)
    {
        // TODO: we'd need to buffer the sub-batch from IEnumerable<T>
        if (batch.IsDefault) return;

        // assert that we currently expect only single commands per element
        if (batch.Command.CommandCount != batch.GroupCount) Throw();

        if (commandFactory.RequirePostProcess)
        {
            batch.Command.UnsafeMoveTo(0);
            foreach (var val in args.Slice(argOffset, batch.GroupCount))
            {
                commandFactory.PostProcess(in batch.Command, val, batch.Command.RecordsAffected);
                batch.Command.UnsafeAdvance();
            }
            argOffset += batch.GroupCount;
        }
        // prepare for the next batch, if one
        batch.UnsafeMoveBeforeFirst();

        static void Throw() => throw new InvalidOperationException("The number of operations should have matched the number of groups!");
    }

    private TArgs[]? GetMultiBatchBuffer(ref int batchSize)
    {
        if (!commandFactory.RequirePostProcess) return null; // no problem, then

        const int MAX_SIZE = 1024;
        if (batchSize < 0 || batchSize > 1024) batchSize = MAX_SIZE;

        return ArrayPool<TArgs>.Shared.Rent(batchSize);
    }
    private static void RecycleMultiBatchBuffer(TArgs[]? buffer)
    {
        if (buffer is not null) ArrayPool<TArgs>.Shared.Return(buffer);
    }

    private int ExecuteMultiBatch(IEnumerable<TArgs> source, int batchSize)
    {
        SyncCommandState state = default;
        var buffer = GetMultiBatchBuffer(ref batchSize);
        try
        {
            int sum = 0, ppOffset = 0;
            foreach (var arg in source)
            {
                Add(ref state.UnifiedBatch, arg);
                if (buffer is not null) buffer[ppOffset++] = arg;

                if (state.UnifiedBatch.GroupCount == batchSize)
                {
                    sum += state.ExecuteNonQueryUnified();
                    PostProcessMultiBatch(in state.UnifiedBatch, buffer);
                    ppOffset = 0;
                }
            }

            if (state.UnifiedBatch.GroupCount != 0)
            {
                state.UnifiedBatch.Trim();
                sum += state.ExecuteNonQueryUnified();
                PostProcessMultiBatch(in state.UnifiedBatch, buffer);
            }
            state.UnifiedBatch.TryRecycle();
            return sum;
        }
        finally
        {
            RecycleMultiBatchBuffer(buffer);
            state.Dispose();
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
                UnifiedCommand cmdState = new(commandFactory, state.Command);
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
                state.UnifiedBatch.TryRecycle();
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
        var state = AsyncCommandState.Create();
        try
        {
            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source.Span[0];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
            UnifiedCommand cmdState = new(commandFactory, state.Command);
            commandFactory.PostProcess(in cmdState, current, local);
            total += local;

            for (int i = 1; i < source.Length; i++)
            {
                current = source.Span[i];
                commandFactory.UpdateParameters(in cmdState, current);
                local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;
            }

            state.UnifiedBatch.TryRecycle();
            return total;
        }
        finally
        {
            await state.DisposeAsync();
            state.Recycle();
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
        AsyncCommandState? state = null;
        var iterator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            int total = 0;
            if (await iterator.MoveNextAsync())
            {
                state ??= AsyncCommandState.Create();

                var current = iterator.Current;
                bool haveMore = await iterator.MoveNextAsync();
                if (haveMore && commandFactory.CanPrepare) state.PrepareBeforeExecute();

                var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
                UnifiedCommand cmdState = new(commandFactory, state.Command);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(in cmdState, current);
                    local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                    commandFactory.PostProcess(in cmdState, current, local);
                    total += local;

                    haveMore = await iterator.MoveNextAsync();
                }
                state.UnifiedBatch.TryRecycle();
            }
            return total;
        }
        finally
        {
            await iterator.DisposeAsync();
            if (state is not null)
            {
                await state.DisposeAsync();
                state.Recycle();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task<int> ExecuteMultiAsync(IEnumerable<TArgs> source, int batchSize, CancellationToken cancellationToken)
    {
#if NET6_0_OR_GREATER
        if (UseBatch(batchSize)) return ExecuteMultiBatchAsync(source, batchSize, cancellationToken);
#endif
        return ExecuteMultiSequentialAsync(source, cancellationToken);
    }

    private async Task<int> ExecuteMultiSequentialAsync(IEnumerable<TArgs> source, CancellationToken cancellationToken)
    {
        AsyncCommandState? state = null;
        var iterator = source.GetEnumerator();
        try
        {
            int total = 0;
            if (iterator.MoveNext())
            {
                state ??= AsyncCommandState.Create();

                var current = iterator.Current;
                bool haveMore = iterator.MoveNext();
                if (haveMore && commandFactory.CanPrepare) state.PrepareBeforeExecute();
                var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
                UnifiedCommand cmdState = new(commandFactory, state.Command);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;

                while (haveMore)
                {
                    current = iterator.Current;
                    commandFactory.UpdateParameters(in cmdState, current);
                    local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                    commandFactory.PostProcess(in cmdState, current, local);
                    total += local;

                    haveMore = iterator.MoveNext();
                }
                state.UnifiedBatch.TryRecycle();
            }
            return total;
        }
        finally
        {
            iterator.Dispose();
            if (state is not null)
            {
                await state.DisposeAsync();
                state.Recycle();
            }
        }
    }

#if NET6_0_OR_GREATER
    private async Task<int> ExecuteMultiBatchAsync(IEnumerable<TArgs> source, int batchSize, CancellationToken cancellationToken)
    {
        AsyncCommandState? state = null;
        var buffer = GetMultiBatchBuffer(ref batchSize);
        try
        {
            int sum = 0, ppOffset = 0;
            foreach (var arg in source)
            {
                state ??= AsyncCommandState.Create();
                Add(ref state.UnifiedBatch, arg);
                if (buffer is not null) buffer[ppOffset++] = arg;

                if (state.UnifiedBatch.GroupCount == batchSize)
                {
                    sum += await state.ExecuteNonQueryUnifiedAsync(cancellationToken);
                    PostProcessMultiBatch(in state.UnifiedBatch, buffer);
                    ppOffset = 0;
                }
            }

            if (state is not null)
            {
                if (state.UnifiedBatch.GroupCount != 0)
                {
                    sum += await state.ExecuteNonQueryUnifiedAsync(cancellationToken);
                    PostProcessMultiBatch(in state.UnifiedBatch, buffer);
                }
                state.UnifiedBatch.TryRecycle();
            }
            return sum;
        }
        finally
        {
            RecycleMultiBatchBuffer(buffer);
            if (state is not null)
            {
                await state.DisposeAsync();
                state.Recycle();
            }
        }
    }
#endif

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
        Debug.Assert(count > 0);
        var state = AsyncCommandState.Create();
        try
        {
            // count is now actually "end"
            count += offset;

            if (commandFactory.CanPrepare) state.PrepareBeforeExecute();
            int total = 0;
            var current = source[offset++];

            var local = await state.ExecuteNonQueryAsync(GetCommand(current), cancellationToken);
            UnifiedCommand cmdState = new(commandFactory, state.Command);
            commandFactory.PostProcess(in cmdState, current, local);
            total += local;

            while (offset < count) // actually "offset < end"
            {
                current = source[offset++];
                commandFactory.UpdateParameters(in cmdState, current);
                local = await state.Command.ExecuteNonQueryAsync(cancellationToken);
                commandFactory.PostProcess(in cmdState, current, local);
                total += local;
            }

            state.UnifiedBatch.TryRecycle();
            return total;
        }
        finally
        {
            await state.DisposeAsync();
            state.Recycle();
        }
    }

#if NET6_0_OR_GREATER
    private async Task<int> ExecuteMultiBatchAsync(TArgs[] source, int offset, int count, int batchSize, CancellationToken cancellationToken)
    {
        Debug.Assert(source.Length > 1);
        AsyncCommandState? state = null;
        var end = offset + count;
        try
        {
            int sum = 0, ppOffset = offset;
            for (int i = offset; i < end; i++)
            {
                state ??= AsyncCommandState.Create();
                Add(ref state.UnifiedBatch, source[i]);
                if (state.UnifiedBatch.GroupCount == batchSize)
                {
                    sum += await state.ExecuteNonQueryUnifiedAsync(cancellationToken);
                    PostProcessMultiBatch(in state.UnifiedBatch, ref ppOffset, source);
                }
            }

            if (state is not null)
            {
                if (state.UnifiedBatch.GroupCount != 0)
                {
                    sum += await state.ExecuteNonQueryUnifiedAsync(cancellationToken);
                    PostProcessMultiBatch(in state.UnifiedBatch, ref ppOffset, source);
                }
                state.UnifiedBatch.TryRecycle();
            }
            return sum;
        }
        finally
        {
            if (state is not null)
            {
                await state.DisposeAsync();
                state.Recycle();
            }
        }
    }
#endif
}

using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

partial struct Command<TArgs>
{
    /// <summary>
    /// Finds multiple split points based on splitOn column names
    /// </summary>
    private static int[] FindSplits(System.Data.Common.DbDataReader reader, string splitOn, int count)
    {
        var splits = new int[count];
        var fieldCount = reader.FieldCount;
        
        // Handle wildcard splitOn - split after every column
        if (splitOn == "*")
        {
            for (int i = 0; i < count; i++)
            {
                splits[i] = i + 1;
            }
            return splits;
        }
        
#if NET5_0_OR_GREATER
        var splitColumns = splitOn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#else
        var splitColumns = splitOn.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int idx = 0; idx < splitColumns.Length; idx++)
        {
            splitColumns[idx] = splitColumns[idx].Trim();
        }
#endif
        
        // Search RIGHT-TO-LEFT like original Dapper to handle duplicate column names correctly
        // "in this we go right to left through the data reader in order to cope with properties that are
        // named the same as a subsequent primary key that we split on"
        int splitIndex = count - 1;
        int currentPos = fieldCount;
        
        // Process splits from last to first
        for (int splitIdx = splitColumns.Length - 1; splitIdx >= 0; splitIdx--)
        {
            if (splitIndex < 0) break;
            
            var split = splitColumns[splitIdx];
            bool found = false;
            
            // Search backwards from currentPos
            for (int i = currentPos - 1; i > 0; i--)
            {
                var name = reader.GetName(i);
                if (string.Equals(name, split, StringComparison.OrdinalIgnoreCase))
                {
                    splits[splitIndex--] = i;
                    currentPos = i;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                // Build helpful error message like original Dapper
                var availableColumns = new System.Text.StringBuilder();
                for (int i = 0; i < fieldCount; i++)
                {
                    if (i > 0) availableColumns.Append(", ");
                    availableColumns.Append(reader.GetName(i));
                }
                throw new System.ArgumentException(
                    $"Multi-map error: splitOn column '{split}' was not found - please ensure your splitOn parameter is set and in the correct order (available columns: {availableColumns})",
                    nameof(splitOn));
            }
        }
        
        // Fill remaining splits evenly if not all found
        // Note: splitIndex will be >= 0 if we didn't find all splits (since we decrement on each find)
        if (splitIndex >= 0)
        {
            var remaining = splitIndex + 1;
            var step = currentPos / (remaining + 1);
            for (int i = 0; i <= splitIndex; i++)
            {
                currentPos -= step;
                if (currentPos <= 0) currentPos = 1; // Ensure we don't go below column 1
                splits[i] = currentPos;
            }
        }
        
        return splits;
    }


    /// <summary>
    /// Reads buffered rows from a multi-map query with 2 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, TReturn>(
        TArgs args,
        Func<T1, T2, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var split = FindSplits(state.Reader, splitOn, 1)[0];
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, split), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(split), split);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, split), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(split), split, tokenState2);
                    results.Add(map(obj1, obj2));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 2 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, TReturn>(
        TArgs args,
        Func<T1, T2, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 1);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0]), splits[0]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0]), splits[0], tokenState2);
                    results.Add(map(obj1, obj2));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 3 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, T3, TReturn>(
        TArgs args,
        Func<T1, T2, T3, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 2);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1]), splits[1]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1]), splits[1], tokenState3);
                    results.Add(map(obj1, obj2, obj3));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 3 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, T3, TReturn>(
        TArgs args,
        Func<T1, T2, T3, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 2);
                var allTokens = state.Lease();
                
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1]), splits[1]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1]), splits[1], tokenState3);
                    results.Add(map(obj1, obj2, obj3));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 4 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, T3, T4, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 3);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2]), splits[2]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2]), splits[2], tokenState4);
                    results.Add(map(obj1, obj2, obj3, obj4));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 4 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, T3, T4, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 3);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2]), splits[2]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2]), splits[2], tokenState4);
                    results.Add(map(obj1, obj2, obj3, obj4));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 5 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, T3, T4, T5, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 4);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3]), splits[3]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3]), splits[3], tokenState5);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 5 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, T3, T4, T5, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 4);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3]), splits[3]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3]), splits[3], tokenState5);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 6 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, T3, T4, T5, T6, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 5);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4]), splits[4]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4]), splits[4], tokenState6);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5, obj6));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 6 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, T3, T4, T5, T6, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 5);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4]), splits[4]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4]), splits[4], tokenState6);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5, obj6));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 7 types
    /// </summary>
    public List<TReturn> QueryBuffered<T1, T2, T3, T4, T5, T6, T7, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, T7, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        [DapperAot] RowFactory<T7>? factory7 = null,
        string splitOn = "Id",
        int rowCountHint = 0)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TReturn> results;
            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 6);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4], splits[5] - splits[4]), splits[4]);
                var tokenState7 = (factory7 ??= RowFactory<T7>.Default).Tokenize(state.Reader, allTokens.Slice(splits[5]), splits[5]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4], splits[5] - splits[4]), splits[4], tokenState6);
                    var obj7 = factory7.Read(state.Reader, allTokensReadOnly.Slice(splits[5]), splits[5], tokenState7);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5, obj6, obj7));
                }
                while (state.Reader.Read());
                state.Return();
            }
            else
            {
                results = [];
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return results;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads buffered rows from a multi-map query with 7 types (async)
    /// </summary>
    public async Task<List<TReturn>> QueryBufferedAsync<T1, T2, T3, T4, T5, T6, T7, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, T7, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        [DapperAot] RowFactory<T7>? factory7 = null,
        string splitOn = "Id",
        int rowCountHint = 0,
        CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TReturn> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 6);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4], splits[5] - splits[4]), splits[4]);
                var tokenState7 = (factory7 ??= RowFactory<T7>.Default).Tokenize(state.Reader, allTokens.Slice(splits[5]), splits[5]);
                
                results = RowFactory.GetRowBuffer<TReturn>(rowCountHint);
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4], splits[5] - splits[4]), splits[4], tokenState6);
                    var obj7 = factory7.Read(state.Reader, allTokensReadOnly.Slice(splits[5]), splits[5], tokenState7);
                    results.Add(map(obj1, obj2, obj3, obj4, obj5, obj6, obj7));
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            else
            {
                results = [];
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return results;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 2 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, TReturn>(
        TArgs args,
        Func<T1, T2, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var split = FindSplits(state.Reader, splitOn, 1)[0];
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, split), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(split), split);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, split), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(split), split, tokenState2);
                    yield return map(obj1, obj2);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 2 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, TReturn>(
        TArgs args,
        Func<T1, T2, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var split = FindSplits(state.Reader, splitOn, 1)[0];
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, split), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(split), split);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, split), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(split), split, tokenState2);
                    yield return map(obj1, obj2);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 3 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, T3, TReturn>(
        TArgs args,
        Func<T1, T2, T3, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 2);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1]), splits[1]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1]), splits[1], tokenState3);
                    yield return map(obj1, obj2, obj3);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 3 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, T3, TReturn>(
        TArgs args,
        Func<T1, T2, T3, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 2);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1]), splits[1]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1]), splits[1], tokenState3);
                    yield return map(obj1, obj2, obj3);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 4 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, T3, T4, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 3);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2]), splits[2]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2]), splits[2], tokenState4);
                    yield return map(obj1, obj2, obj3, obj4);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 4 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, T3, T4, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 3);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2]), splits[2]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2]), splits[2], tokenState4);
                    yield return map(obj1, obj2, obj3, obj4);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 5 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, T3, T4, T5, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 4);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3]), splits[3]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3]), splits[3], tokenState5);
                    yield return map(obj1, obj2, obj3, obj4, obj5);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 5 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, T3, T4, T5, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 4);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3]), splits[3]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3]), splits[3], tokenState5);
                    yield return map(obj1, obj2, obj3, obj4, obj5);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 6 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, T3, T4, T5, T6, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 5);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4]), splits[4]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4]), splits[4], tokenState6);
                    yield return map(obj1, obj2, obj3, obj4, obj5, obj6);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 6 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, T3, T4, T5, T6, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 5);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4]), splits[4]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4]), splits[4], tokenState6);
                    yield return map(obj1, obj2, obj3, obj4, obj5, obj6);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 7 types
    /// </summary>
    public IEnumerable<TReturn> QueryUnbuffered<T1, T2, T3, T4, T5, T6, T7, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, T7, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        [DapperAot] RowFactory<T7>? factory7 = null,
        string splitOn = "Id")
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var splits = FindSplits(state.Reader, splitOn, 6);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4], splits[5] - splits[4]), splits[4]);
                var tokenState7 = (factory7 ??= RowFactory<T7>.Default).Tokenize(state.Reader, allTokens.Slice(splits[5]), splits[5]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4], splits[5] - splits[4]), splits[4], tokenState6);
                    var obj7 = factory7.Read(state.Reader, allTokensReadOnly.Slice(splits[5]), splits[5], tokenState7);
                    yield return map(obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                }
                while (state.Reader.Read());
                state.Return();
            }

            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a multi-map query with 7 types (async)
    /// </summary>
    public async IAsyncEnumerable<TReturn> QueryUnbufferedAsync<T1, T2, T3, T4, T5, T6, T7, TReturn>(
        TArgs args,
        Func<T1, T2, T3, T4, T5, T6, T7, TReturn> map,
        [DapperAot] RowFactory<T1>? factory1 = null,
        [DapperAot] RowFactory<T2>? factory2 = null,
        [DapperAot] RowFactory<T3>? factory3 = null,
        [DapperAot] RowFactory<T4>? factory4 = null,
        [DapperAot] RowFactory<T5>? factory5 = null,
        [DapperAot] RowFactory<T6>? factory6 = null,
        [DapperAot] RowFactory<T7>? factory7 = null,
        string splitOn = "Id",
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var splits = FindSplits(state.Reader, splitOn, 6);
                var allTokens = state.Lease();
                var tokenState1 = (factory1 ??= RowFactory<T1>.Default).Tokenize(state.Reader, allTokens.Slice(0, splits[0]), 0);
                var tokenState2 = (factory2 ??= RowFactory<T2>.Default).Tokenize(state.Reader, allTokens.Slice(splits[0], splits[1] - splits[0]), splits[0]);
                var tokenState3 = (factory3 ??= RowFactory<T3>.Default).Tokenize(state.Reader, allTokens.Slice(splits[1], splits[2] - splits[1]), splits[1]);
                var tokenState4 = (factory4 ??= RowFactory<T4>.Default).Tokenize(state.Reader, allTokens.Slice(splits[2], splits[3] - splits[2]), splits[2]);
                var tokenState5 = (factory5 ??= RowFactory<T5>.Default).Tokenize(state.Reader, allTokens.Slice(splits[3], splits[4] - splits[3]), splits[3]);
                var tokenState6 = (factory6 ??= RowFactory<T6>.Default).Tokenize(state.Reader, allTokens.Slice(splits[4], splits[5] - splits[4]), splits[4]);
                var tokenState7 = (factory7 ??= RowFactory<T7>.Default).Tokenize(state.Reader, allTokens.Slice(splits[5]), splits[5]);
                
                do
                {
                    var allTokensReadOnly = state.Tokens;
                    var obj1 = factory1.Read(state.Reader, allTokensReadOnly.Slice(0, splits[0]), 0, tokenState1);
                    var obj2 = factory2.Read(state.Reader, allTokensReadOnly.Slice(splits[0], splits[1] - splits[0]), splits[0], tokenState2);
                    var obj3 = factory3.Read(state.Reader, allTokensReadOnly.Slice(splits[1], splits[2] - splits[1]), splits[1], tokenState3);
                    var obj4 = factory4.Read(state.Reader, allTokensReadOnly.Slice(splits[2], splits[3] - splits[2]), splits[2], tokenState4);
                    var obj5 = factory5.Read(state.Reader, allTokensReadOnly.Slice(splits[3], splits[4] - splits[3]), splits[3], tokenState5);
                    var obj6 = factory6.Read(state.Reader, allTokensReadOnly.Slice(splits[4], splits[5] - splits[4]), splits[4], tokenState6);
                    var obj7 = factory7.Read(state.Reader, allTokensReadOnly.Slice(splits[5]), splits[5], tokenState7);
                    yield return map(obj1, obj2, obj3, obj4, obj5, obj6, obj7);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }

            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}


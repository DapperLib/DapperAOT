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
    /// Reads rows from a query, optionally buffered
    /// </summary>
    public IEnumerable<TRow> Query<TRow>(TArgs args, bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => buffered ? QueryBuffered(args, rowFactory) : QueryUnbuffered(args, rowFactory);

    /// <summary>
    /// Reads buffered rows from a query
    /// </summary>
    public List<TRow> QueryBuffered<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, int rowCountHint = 0)

    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            List<TRow> results;
            if (state.Reader.Read())
            {
                var readWriteTokens = state.Reader.FieldCount <= RowFactory.MAX_STACK_TOKENS
                    ? CommandUtils.UnsafeSlice(stackalloc int[RowFactory.MAX_STACK_TOKENS], state.Reader.FieldCount)
                    : state.Lease();

                var tokenState = (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, readWriteTokens, 0);
                results = RowFactory.GetRowBuffer<TRow>(rowCountHint);
                ReadOnlySpan<int> readOnlyTokens = readWriteTokens; // avoid multiple conversions
                do
                {
                    results.Add(rowFactory.Read(state.Reader, readOnlyTokens, 0, tokenState));
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
    /// Reads buffered rows from a query
    /// </summary>
    public async Task<List<TRow>> QueryBufferedAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null,
        int rowCountHint = 0, CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            List<TRow> results;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var tokenState = (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                results = RowFactory.GetRowBuffer<TRow>(rowCountHint);
                do
                {
                    results.Add(rowFactory.Read(state.Reader, state.Tokens, 0, tokenState));
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
    /// Reads unbuffered rows from a query
    /// </summary>
    public async IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AsyncQueryState state = new();
        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess, cancellationToken);

            if (await state.Reader.ReadAsync(cancellationToken))
            {
                var tokenState = (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                do
                {
                    yield return rowFactory.Read(state.Reader, state.Tokens, 0, tokenState);
                }
                while (await state.Reader.ReadAsync(cancellationToken));
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await state.Reader.NextResultAsync(cancellationToken)) { }
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads unbuffered rows from a query
    /// </summary>
    public IEnumerable<TRow> QueryUnbuffered<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);

            if (state.Reader.Read())
            {
                var tokenState = (rowFactory ??= RowFactory<TRow>.Default).Tokenize(state.Reader, state.Lease(), 0);
                do
                {
                    yield return rowFactory.Read(state.Reader, state.Tokens, 0, tokenState);
                }
                while (state.Reader.Read());
                state.Return();
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (state.Reader.NextResult()) { }
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
        }
        finally
        {
            state.Dispose();
        }
    }

    // if we don't care if there's two rows, we can restrict to read one only
    static CommandBehavior SingleFlags(OneRowFlags flags)
        => (flags & OneRowFlags.ThrowIfMultiple) == 0
            ? CommandBehavior.SingleResult | CommandBehavior.SequentialAccess | CommandBehavior.SingleRow
            : CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;

    private TRow? QueryOneRow<TRow>(
        TArgs args,
        OneRowFlags flags,
        RowFactory<TRow>? rowFactory)
    {
        SyncQueryState state = default;
        try
        {
            state.ExecuteReader(GetCommand(args), SingleFlags(flags));

            TRow? result = default;
            if (state.Reader.Read())
            {
                result = (rowFactory ??= RowFactory<TRow>.Default).Read(state.Reader, ref state.Leased);
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
            PostProcessAndRecycle(ref state, args, state.Reader.CloseAndCapture());
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    private async Task<TRow?> QueryOneRowAsync<TRow>(
        TArgs args,
        OneRowFlags flags,
        RowFactory<TRow>? rowFactory,
        CancellationToken cancellationToken)
    {
        AsyncQueryState state = new();

        try
        {
            cancellationToken = GetCancellationToken(args, cancellationToken);
            await state.ExecuteReaderAsync(GetCommand(args), SingleFlags(flags), cancellationToken);

            TRow? result = default;
            if (await state.Reader.ReadAsync(cancellationToken))
            {
                result = (rowFactory ??= RowFactory<TRow>.Default).Read(state.Reader, ref state.Leased);
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
            PostProcessAndRecycle(state, args, await state.Reader.CloseAndCaptureAsync());
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Reads exactly one row
    /// </summary>
    public TRow QuerySingle<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(args, OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory)!;

    /// <summary>
    /// Reads the first row returned
    /// </summary>
    public TRow QueryFirst<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(args, OneRowFlags.ThrowIfNone, rowFactory)!;

    /// <summary>
    /// Reads at most one row
    /// </summary>
    public TRow? QuerySingleOrDefault<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(args, OneRowFlags.ThrowIfMultiple, rowFactory);

    /// <summary>
    /// Reads the first row, if any are returned
    /// </summary>
    public TRow? QueryFirstOrDefault<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(args, OneRowFlags.None, rowFactory);

    /// <summary>
    /// Reads exactly one row
    /// </summary>
    public Task<TRow> QuerySingleAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(args, OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken)!;

    /// <summary>
    /// Reads the first row returned
    /// </summary>
    public Task<TRow> QueryFirstAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(args, OneRowFlags.ThrowIfNone, rowFactory, cancellationToken)!;

    /// <summary>
    /// Reads at most one row
    /// </summary>
    public Task<TRow?> QuerySingleOrDefaultAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(args, OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken);

    /// <summary>
    /// Reads the first row, if any are returned
    /// </summary>
    public Task<TRow?> QueryFirstOrDefaultAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(args, OneRowFlags.None, rowFactory, cancellationToken);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Abstraction of <see cref="Command{TArgs}"/> intended for mocking scenarios; it is not intended
/// for consumers to implement this API directly, and it is considered allowable for the library
/// authors to arbitrarily change this API; no apology will be offered for this. In the unlikely
/// scenario that you use the library API, <see cref="Command{TArgs}"/> should be preferred except
/// for mocks in unit tests, which may use <see cref="ICommand{TArgs}"/>.
/// </summary>
public interface ICommand<TArgs>
{
    /// <inheritdoc cref="Command{TArgs}.QuerySingle{TRow}(TArgs, RowFactory{TRow}?)"/>
    TRow QuerySingle<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirst{TRow}(TArgs, RowFactory{TRow}?)"/>
    TRow QueryFirst<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefault{TRow}(TArgs, RowFactory{TRow}?)"/>
    TRow? QuerySingleOrDefault<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefault{TRow}(TArgs, RowFactory{TRow}?)"/>
    TRow? QueryFirstOrDefault<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleAsync{TRow}(TArgs, RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow> QuerySingleAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstAsync{TRow}(TArgs, RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow> QueryFirstAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefaultAsync{TRow}(TArgs, RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow?> QuerySingleOrDefaultAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefaultAsync{TRow}(TArgs, RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow?> QueryFirstOrDefaultAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Execute(TArgs)"/>
    int Execute(TArgs args);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(TArgs, CancellationToken)"/>
    Task<int> ExecuteAsync(TArgs args, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar(TArgs)"/>
    object? ExecuteScalar(TArgs args);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync(TArgs, CancellationToken)"/>
    Task<object?> ExecuteScalarAsync(TArgs args, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar{T}(TArgs)"/>
    T ExecuteScalar<T>(TArgs args);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync{T}(TArgs, CancellationToken)"/>
    Task<T> ExecuteScalarAsync<T>(TArgs args, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Query{TRow}(TArgs, bool, RowFactory{TRow}?)"/>
    IEnumerable<TRow> Query<TRow>(TArgs args, bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryBuffered{TRow}(TArgs, RowFactory{TRow}?, int)"/>
    List<TRow> QueryBuffered<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, int rowCountHint = 0);

    /// <inheritdoc cref="Command{TArgs}.QueryBufferedAsync{TRow}(TArgs, RowFactory{TRow}?, int, CancellationToken)"/>
    Task<List<TRow>> QueryBufferedAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null, int rowCountHint = 0, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbufferedAsync{TRow}(TArgs, RowFactory{TRow}?, CancellationToken)"/>
    IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbuffered{TRow}(TArgs, RowFactory{TRow}?)"/>
    IEnumerable<TRow> QueryUnbuffered<TRow>(TArgs args, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.Execute(TArgs[], int)"/>
    int Execute(TArgs[] values, int batchSize = -1);

    /// <inheritdoc cref="Command{TArgs}.Execute(IEnumerable{TArgs}, int)"/>
    int Execute(IEnumerable<TArgs> values, int batchSize = -1);

    /// <inheritdoc cref="Command{TArgs}.Execute(ReadOnlySpan{TArgs}, int)"/>
    int Execute(ReadOnlySpan<TArgs> values, int batchSize = -1);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(TArgs[], int, CancellationToken)"/>
    Task<int> ExecuteAsync(TArgs[] values, int batchSize = -1, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(IEnumerable{TArgs}, int, CancellationToken)"/>
    Task<int> ExecuteAsync(IEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(IAsyncEnumerable{TArgs}, int, CancellationToken)"/>
    Task<int> ExecuteAsync(IAsyncEnumerable<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(ReadOnlyMemory{TArgs}, int, CancellationToken)"/>
    Task<int> ExecuteAsync(ReadOnlyMemory<TArgs> values, int batchSize = -1, CancellationToken cancellationToken = default);
}
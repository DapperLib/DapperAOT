using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal;

// this describes the APIs that should be available on Command and Command<T>, to
// ensure they are consistent; this is not meant for public use - it is mostly
// a cheeky compiler check!
#if DEBUG

internal interface ICommandApi
{
    /// <inheritdoc cref="Command{TArgs}.QuerySingle{TRow}(RowFactory{TRow}?)"/>
    TRow QuerySingle<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirst{TRow}(RowFactory{TRow}?)"/>
    TRow QueryFirst<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefault{TRow}(RowFactory{TRow}?)"/>
    TRow? QuerySingleOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefault{TRow}(RowFactory{TRow}?)"/>
    TRow? QueryFirstOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow> QuerySingleAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow> QueryFirstAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefaultAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow?> QuerySingleOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefaultAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    Task<TRow?> QueryFirstOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Execute()"/>
    int Execute();

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(CancellationToken)"/>
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar()"/>
    object? ExecuteScalar();

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync(CancellationToken)"/>
    Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar{T}()"/>
    T ExecuteScalar<T>();

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync{T}(CancellationToken)"/>
    Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Query{TRow}(bool, RowFactory{TRow}?)"/>
    IEnumerable<TRow> Query<TRow>(bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryBuffered{TRow}(RowFactory{TRow}?)"/>
    List<TRow> QueryBuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryBufferedAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbufferedAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbuffered{TRow}(RowFactory{TRow}?)"/>
    IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);
}
#endif
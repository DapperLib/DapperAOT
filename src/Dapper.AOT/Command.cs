using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Represents the state required to perform an ADO.NET operation
/// </summary>
public abstract class Command
#if DEBUG
    : Dapper.Internal.ICommandApi
#endif
{
    /// <inheritdoc cref="Command{TArgs}.QuerySingle{TRow}(RowFactory{TRow}?)"/>
    public abstract TRow QuerySingle<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirst{TRow}(RowFactory{TRow}?)"/>
    public abstract TRow QueryFirst<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefault{TRow}(RowFactory{TRow}?)"/>
    public abstract TRow? QuerySingleOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefault{TRow}(RowFactory{TRow}?)"/>
    public abstract TRow? QueryFirstOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract Task<TRow> QuerySingleAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract Task<TRow> QueryFirstAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QuerySingleOrDefaultAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract Task<TRow?> QuerySingleOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryFirstOrDefaultAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract Task<TRow?> QueryFirstOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Execute()"/>
    public abstract int Execute();

    /// <inheritdoc cref="Command{TArgs}.ExecuteAsync(CancellationToken)"/>
    public abstract Task<int> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar()"/>
    public abstract object? ExecuteScalar();

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync(CancellationToken)"/>
    public abstract Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalar{T}()"/>
    public abstract T ExecuteScalar<T>();

    /// <inheritdoc cref="Command{TArgs}.ExecuteScalarAsync{T}(CancellationToken)"/>
    public abstract Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.Query{TRow}(bool, RowFactory{TRow}?)"/>
    public IEnumerable<TRow> Query<TRow>(bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => buffered ? QueryBuffered(rowFactory) : QueryUnbuffered(rowFactory);

    /// <inheritdoc cref="Command{TArgs}.QueryBuffered{TRow}(RowFactory{TRow}?)"/>
    public abstract List<TRow> QueryBuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    /// <inheritdoc cref="Command{TArgs}.QueryBufferedAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbufferedAsync{TRow}(RowFactory{TRow}?, CancellationToken)"/>
    public abstract IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="Command{TArgs}.QueryUnbuffered{TRow}(RowFactory{TRow}?)"/>
    public abstract IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null);

    internal sealed class TypedCommand<TArgs> : Command
    {
        private readonly Command<TArgs> command;

        public TypedCommand(Command<TArgs> command) => this.command = command;

        public override int Execute() => command.Execute();

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => command.ExecuteAsync(cancellationToken);

        public override object? ExecuteScalar() => command.ExecuteScalar();

        public override T ExecuteScalar<T>() => command.ExecuteScalar<T>();

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) => command.ExecuteScalarAsync(cancellationToken);

        public override Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken = default) => command.ExecuteScalarAsync<T>(cancellationToken);

        public override List<TRow> QueryBuffered<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) => command.QueryBuffered(rowFactory);

        public override Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) => command.QueryBufferedAsync(rowFactory, cancellationToken);

        public override TRow QueryFirst<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) => command.QueryFirst(rowFactory);

        public override Task<TRow> QueryFirstAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) => command.QueryFirstAsync(rowFactory, cancellationToken);

        public override TRow? QueryFirstOrDefault<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) where TRow : default => command.QueryFirstOrDefault(rowFactory);

        public override Task<TRow?> QueryFirstOrDefaultAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) where TRow : default => command.QueryFirstOrDefaultAsync(rowFactory, cancellationToken);

        public override TRow QuerySingle<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) => command.QuerySingle(rowFactory);

        public override Task<TRow> QuerySingleAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) => command.QuerySingleAsync(rowFactory, cancellationToken);

        public override TRow? QuerySingleOrDefault<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) where TRow : default => command.QuerySingleOrDefault(rowFactory);

        public override Task<TRow?> QuerySingleOrDefaultAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) where TRow : default => command.QuerySingleOrDefaultAsync(rowFactory, cancellationToken);

        public override IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null) => command.QueryUnbuffered(rowFactory);

        public override IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot(true)] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default) => command.QueryUnbufferedAsync(rowFactory, cancellationToken);
    }
}
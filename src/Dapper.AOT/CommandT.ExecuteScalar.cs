using Dapper.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

partial struct Command<TArgs>
{
    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public object? ExecuteScalar(TArgs args)
    {
        CommandState state = default;
        try
        {
            var result = state.ExecuteScalar(GetCommand(args));
            PostProcessAndRecycle(ref state, args);
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<object?> ExecuteScalarAsync(TArgs args, CancellationToken cancellationToken = default)
    {
        CommandState state = default;
        try
        {
            var result = await state.ExecuteScalarAsync(GetCommand(args), cancellationToken);
            PostProcessAndRecycle(ref state, args);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public T ExecuteScalar<T>(TArgs args)
    {
        CommandState state = default;
        try
        {
            var result = state.ExecuteScalar(GetCommand(args));
            PostProcessAndRecycle(ref state, args);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Read the first cell of data fetched from a query
    /// </summary>
    public async Task<T> ExecuteScalarAsync<T>(TArgs args, CancellationToken cancellationToken = default)
    {
        CommandState state = default;
        try
        {
            var result = await state.ExecuteScalarAsync(GetCommand(args), cancellationToken);
            PostProcessAndRecycle(ref state, args);
            return CommandUtils.As<T>(result);
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}

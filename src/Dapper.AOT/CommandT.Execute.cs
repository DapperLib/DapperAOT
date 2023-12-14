using Dapper.Internal;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

partial struct Command<TArgs>
{
    /// <summary>
    /// Executes a non-query operation
    /// </summary>
    public int Execute(TArgs args)
    {
        SyncCommandState state = default;
        try
        {
            var result = state.ExecuteNonQuery(GetCommand(args));
            PostProcessAndRecycle(ref state, args, result);
            return result;
        }
        finally
        {
            state.Dispose();
        }
    }

    /// <summary>
    /// Executes a non-query operation
    /// </summary>
    public async Task<int> ExecuteAsync(TArgs args, CancellationToken cancellationToken = default)
    {
        AsyncCommandState state = new();
        try
        {
            var result = await state.ExecuteNonQueryAsync(GetCommand(args), GetCancellationToken(args, cancellationToken));
            PostProcessAndRecycle(state, args, result);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}

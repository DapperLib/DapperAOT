using Dapper.Internal;
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
        CommandState state = default;
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
        CommandState state = default;
        try
        {
            var result = await state.ExecuteNonQueryAsync(GetCommand(args), cancellationToken);
            PostProcessAndRecycle(ref state, args, result);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
        }
    }
}

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
        SyncCommandState state = default;
        try
        {
            GetUnifiedBatch(out state.UnifiedBatch, args);
            var result = state.ExecuteNonQueryUnified();
            PostProcessAndRecycleUnified(state.UnifiedBatch, args, result);
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
        var state = AsyncCommandState.Create();
        try
        {
            GetUnifiedBatch(out state.UnifiedBatch, args);
            var result = await state.ExecuteNonQueryUnifiedAsync(cancellationToken);
            PostProcessAndRecycleUnified(in state.UnifiedBatch, args, result);
            return result;
        }
        finally
        {
            await state.DisposeAsync();
            state.Recycle();
        }
    }
}

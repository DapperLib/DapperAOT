using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP045 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task CancellationDuplicated() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;

        [DapperAot(true)]
        class WithAot
        {
            public async Task DapperVanillaNotSupported(DbConnection conn, int id, CancellationToken cancellationToken)
            {
                _ = conn.ExecuteAsync("some_proc", cancellationToken);
                _ = conn.ExecuteAsync("some_proc", new { id, x = cancellationToken });
                _ = conn.ExecuteAsync("some_proc", new { id, x = cancellationToken, {|#0:y|} = cancellationToken});
            }
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.CancellationDuplicated).WithLocation(0),
        ]
    );

}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP044 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task CancellationNotSupported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;

        [DapperAot(true)]
        class WithAot
        {
            public async Task DapperVanillaNotSupported(DbConnection conn, int id, CancellationToken cancellationToken)
            {
                _ = conn.QueryAsync<int>("some_proc", cancellationToken);
                _ = conn.ExecuteAsync("some_proc", new { id, cancellationToken });
            }
        }
        [DapperAot(false)]
        class WithoutAot
        {
            public async Task DapperVanillaNotSupported(DbConnection conn, int id, CancellationToken cancellationToken)
            {
                _ = conn.QueryAsync<int>("some_proc", {|#0:cancellationToken|});
                _ = conn.ExecuteAsync("some_proc", {|#1:new { id, cancellationToken }|});
            }
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.CancellationNotSupported).WithLocation(0),
            Diagnostic(Diagnostics.CancellationNotSupported).WithLocation(1),
        ]
    );

}
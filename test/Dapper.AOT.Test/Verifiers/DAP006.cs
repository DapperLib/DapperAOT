using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP006 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DapperLegacyTupleParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        class SomeCode
        {
            [DapperAot(false)]
            public void LegacyMode(DbConnection conn)
            {
                conn.Execute("somesql", {|#0:(id: 42, name: "abc")|});
                conn.Execute("somesql", new { id = 42, name = "abc" });
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperLegacyTupleParameter).WithLocation(0)]);

}
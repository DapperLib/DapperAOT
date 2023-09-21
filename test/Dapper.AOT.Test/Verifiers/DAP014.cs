using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP014 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DapperAotTupleParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot, BindTupleByName]
        class SomeCode
        {
            public void NotSupported(DbConnection conn)
            {
                conn.Execute("somesql", {|#0:(id: 42, name: "abc")|});
            }
            public void Workaround(DbConnection conn)
            {
                conn.Execute("somesql", new { id = 42, name = "abc" });
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperAotTupleParameter).WithLocation(0)]);

}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP052 : Verifier<DapperAnalyzer>
{
    [Fact] // the referenced (packaged) Dapper does not expose AddParameters(IDbCommand),
    // so the bag call-site is refused with a message naming exactly what is missing
    public Task DynamicParametersNeedsNewerDapper() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                var bag = new DynamicParameters();
                bag.Add("id", 42);
                conn.Execute("somesql", {|#0:bag|});
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.FeatureNeedsNewerDapper).WithLocation(0)
                .WithArguments("DynamicParameters", "DynamicParameters.AddParameters(IDbCommand)"),
    ]);
}

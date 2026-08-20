using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP052 : Verifier<DapperAnalyzer>
{
    // the last Dapper release WITHOUT AddParameters(IDbCommand); pinned so this test keeps
    // guarding the probe-and-refuse path after the project-wide Dapper reference gains the
    // API (at which point a positive twin - same code, live reference, no diagnostic -
    // becomes writable for the first time)
    private const string DapperWithoutTheApi = "2.1.72";

    [Fact] // a Dapper without AddParameters(IDbCommand): the bag call-site is refused
    // with a message naming exactly what is missing
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
    ], pinDapperPackageVersion: DapperWithoutTheApi);
}

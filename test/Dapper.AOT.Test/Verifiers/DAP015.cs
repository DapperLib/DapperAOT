using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP015 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UntypedParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void WithObject(DbConnection conn, object args) => conn.Execute("somesql", {|#0:args|});
            public void WithDynamicParameters(DbConnection conn, DynamicParameters args) => conn.Execute("somesql", {|#1:args|});
            public void WithCustomType(DbConnection conn, Customer args) => conn.Execute("somesql", args);

            [DapperAot(false)]
            public void DisabledWithObject(DbConnection conn, object args) => conn.Execute("somesql", args);
            [DapperAot(false)]
            public void DisabledWithDynamicParameters(DbConnection conn, DynamicParameters args) => conn.Execute("somesql", args);

            public class Customer {}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UntypedParameter).WithLocation(0),
            // DynamicParameters is no longer 'untyped': it gets the specific needs-newer-Dapper
            // guidance (or works outright, once the referenced Dapper has the self-apply API)
            Diagnostic(Diagnostics.FeatureNeedsNewerDapper).WithLocation(1)
                .WithArguments("DynamicParameters", "DynamicParameters.AddParameters(IDbCommand)")]);

}
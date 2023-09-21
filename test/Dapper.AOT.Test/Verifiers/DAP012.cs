using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP012 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DapperAotAddBindTupleByName() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        #pragma warning disable DAP014 // feature not supported yet
        [DapperAot(true)]
        class SomeCode
        {
            public void Unclear(DbConnection conn)
            {
                conn.Execute("somesql", {|#0:(id: 42, name: "abc")|});
            }
            [BindTupleByName]
            public void Explicit(DbConnection conn)
            {
                conn.Execute("somesql", (id: 42, name: "abc"));
            }
            [BindTupleByName(false)]
            public void ExplicitOff(DbConnection conn)
            {
                conn.Execute("somesql", (id: 42, name: "abc"));
            }
        }
        #pragma warning restore DAP014
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperAotAddBindTupleByName).WithLocation(0)]);

}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP046 : Verifier<DapperAnalyzer>
{
    [Theory]
    [InlineData("[DapperAot(true)]")]
    [InlineData("[DapperAot(false)]")]
    public Task AmbiguousProperties(string dapperAotAttribute) => CSVerifyAsync($$"""
        using Dapper;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;

        public class Data
        {
            public string firstName; // THIS IS FINE AND SHOULD NOT REPORT DIAGNOSTIC -> IT IS A FIELD,

            public string {|#0:First_Name|} { get; set; }
            public string {|#1:FirstName|} { get; set; }
        }

        {{dapperAotAttribute}}
        class WithoutAot
        {
            public void DapperCode(DbConnection conn)
            {
                _ = conn.Query<Data>("select_proc");
            }
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.AmbiguousProperties)
                .WithArguments("firstname")
                .WithLocation(0).WithLocation(1)
        ]
    );
}
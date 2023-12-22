using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP047 : Verifier<DapperAnalyzer>
{
    [Theory]
    [InlineData("[DapperAot(true)]")]
    [InlineData("[DapperAot(false)]")]
    public Task AmbiguousFields(string dapperAotAttribute) => CSVerifyAsync($$"""
        using Dapper;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;

        public class Data
        {
            public string FirstName { get; set; } // THIS IS FINE AND SHOULD NOT REPORT DIAGNOSTIC -> IT IS A PROPERTY,

            public string {|#0:first_name|};
            public string {|#1:_firstName|};
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
            Diagnostic(Diagnostics.AmbiguousFields)
                .WithArguments("firstname")
                .WithLocation(0).WithLocation(1)
        ]
    );
}
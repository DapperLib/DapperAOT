using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP007 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UnexpectedCommandType() => CSVerifyAsync("""
        using Dapper;
        using System.Data;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void SpecifiedButInvalid(DbConnection conn)
            {
                conn.Execute("somesql", {|#0:commandType: (CommandType)42|});
            }
            public void ExplicitCorrect(DbConnection conn)
            {
                conn.Execute("somesql", commandType: CommandType.StoredProcedure);
            }
            public void NotSpecified(DbConnection conn)
            {
                conn.Execute("somesql");
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.UnexpectedCommandType).WithLocation(0)]);
}
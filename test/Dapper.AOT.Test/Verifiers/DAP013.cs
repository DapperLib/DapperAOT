using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP011 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DapperLegacyTupleParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(false)]
        class SomeCode
        {
            [BindTupleByName]
            public void EnabledWithoutNames(DbConnection conn)
            {
                _ = conn.Query<(string,int)>("somesql");
            }
            [BindTupleByName]
            public void EnabledWithNames(DbConnection conn)
            {
                _ = conn.{|#0:Query<(string s,int a)>|}("somesql");
            }
            public void NotEnabled(DbConnection conn)
            {
                _ = conn.Query<(string,int)>("somesql");
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperLegacyBindNameTupleResults).WithLocation(0)]);

}
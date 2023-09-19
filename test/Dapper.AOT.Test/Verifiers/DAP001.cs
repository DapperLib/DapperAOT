using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP001 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UnsupportedMethod() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.{|#0:Query<int,int,int>|}("proc", null!);
                _ = conn.Query("proc");
            }
        }
        """, DefaultConfig,
        [Diagnostic(Diagnostics.UnsupportedMethod).WithLocation(0)
            .WithArguments("SqlMapper.Query<int, int, int>(IDbConnection, string, Func<int, int, int>, object?, IDbTransaction?, bool, string, int?, CommandType?)")]);
}
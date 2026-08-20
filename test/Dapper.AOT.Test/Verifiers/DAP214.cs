using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP214 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableNotDeclared() => SqlVerifyAsync("""
        select {|#0:@i|};
        """, SqlAnalysis.SqlParseInputFlags.KnownParameters, Diagnostic(Diagnostics.VariableNotDeclared).WithLocation(0).WithArguments("@i"));

    [Fact]
    public Task NoFalsePositive_Issue2181() => CSVerifyAsync(""""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection connection)
                => connection.QuerySingle<int>(
                    """select Id from Users where UserTypeId = {=Admin}""",
                    new { Admin = 1 });
        }
        """", DefaultConfig, []);

    [Fact]
    public Task NoFalsePositive_BoundAndLiteralTokens() => CSVerifyAsync(""""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection connection)
                => connection.QuerySingle<int>(
                    """select Id from Users where UserTypeId = {=Admin} and A = @a""",
                    new { Admin = 1, a = 2 });
        }
        """", DefaultConfig, []);
}

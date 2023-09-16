using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP005 : Verifier
{
    [Fact]
    public async Task ShouldDetectNotEnabledWhenUsed()
    {
        var input = """
using Dapper;
using System.Data.Common;

class SomeCode
{
    public void Foo(DbConnection conn) => conn.Execute("some sql");
}
""";

        var expected = Diagnostic(DapperInterceptorGenerator.Diagnostics.DapperAotNotEnabled);
        await VerifyAsync<WrappedDapperInterceptorAnalyzer>(input, expected);
    }
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
// using Verifier = Dapper.AOT.Test.AnalyzerAndCodeFixVerifier<Dapper.AOT.Test.TestCommon.WrappedDapperInterceptorAnalyzer, Dapper.CodeAnalysis.DapperCodeFixes>;
using Verifier = Dapper.AOT.Test.AnalyzerVerifier<Dapper.AOT.Test.TestCommon.WrappedDapperInterceptorAnalyzer>;

namespace Dapper.AOT.Test.Verifiers;

public class DAP005
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

        var expectedError = Verifier.Diagnostic(DapperInterceptorGenerator.Diagnostics.DapperAotNotEnabled.Id);
        await Verifier.VerifyAnalyzerAsync(input, expectedError);
    }
}
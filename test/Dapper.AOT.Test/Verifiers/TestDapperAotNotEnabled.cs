
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
// using Verifier = Dapper.AOT.Test.AnalyzerAndCodeFixVerifier<Dapper.CodeAnalysis.OpinionatedAnalyzer, Dapper.CodeAnalysis.DapperCodeFixes>;
using Verifier = Dapper.AOT.Test.AnalyzerVerifier<Dapper.CodeAnalysis.OpinionatedAnalyzer>;

namespace Dapper.AOT.Test.Verifiers;

public class TestDapperAotNotEnabled
{
    [Fact]
    public async Task ShouldDetectNotEnabledWhenUsed()
    {
        var input = """
using Dapper;
using System.Data.Common;

class SomeCode
{
    public void Foo(DbConnection conn) => conn.{|#0:Execute|}("some sql");
}
""";

        var expectedError = Verifier.Diagnostic(Diagnostics.DapperAotNotEnabled.Id)
                                    .WithLocation(0)
                                    .WithArguments("Execute");
        await Verifier.VerifyAnalyzerAsync(input, expectedError);
    }
}
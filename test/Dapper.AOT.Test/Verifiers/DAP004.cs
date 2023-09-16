using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP004 : Verifier<WrappedDapperInterceptorAnalyzer>
{
    [Fact]
    public Task LanguageTooLow() => VerifyAsync("""
using Dapper;
using System.Data.Common;

[DapperAot(true)]
class SomeCode
{
    public void Foo(DbConnection conn) => conn.Execute("some sql");
}
""", new[]
    {
        InterceptorsEnabled, WithLanguageVersion(LanguageVersion.CSharp10)
    }, Diagnostic(DapperInterceptorGenerator.Diagnostics.LanguageVersionTooLow));

    [Fact]
    public Task FineIfInactive() => VerifyAsync("""
using Dapper;
using System.Data.Common;

[DapperAot(false)]
class SomeCode
{
    public void Foo(DbConnection conn) => conn.Execute("some sql");
}
""", new[]
    {
        InterceptorsEnabled, WithLanguageVersion(LanguageVersion.CSharp10)
    });

    // we don't need to test higher-level language versions: *every other test does that!*
}
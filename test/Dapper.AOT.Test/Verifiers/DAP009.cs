using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP009 : Verifier<WrappedDapperInterceptorAnalyzer>
{
    [Fact]
    public Task LanguagesTooLow() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("some sql");
        }
        """,
        [InterceptorsEnabled, WithCSharpLanguageVersion(LanguageVersion.CSharp10)],
        [Diagnostic(DapperInterceptorGenerator.Diagnostics.LanguageVersionTooLow)]);

}
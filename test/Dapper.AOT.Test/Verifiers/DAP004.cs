using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP004 : Verifier<WrappedDapperInterceptorAnalyzer>
{
    [Fact]
    public Task LanguageTooLow() => CSVerifyAsync("""
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

    [Fact]
    public Task CSFineIfInactive() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(false)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("some sql");
        }
        """, [InterceptorsEnabled, WithCSharpLanguageVersion(LanguageVersion.CSharp10)], []);

    [Fact]
    public Task VBFineIfInactive() => VBVerifyAsync("""
        Imports Dapper
        Imports System.Data.Common

        ' [DapperAot(false)]
        Module SomeCode
            Public Sub Foo(conn As DbConnection)
                conn.Execute("some sql")
            End Sub
        End Module
        """, [InterceptorsEnabled, WithCSharpLanguageVersion(LanguageVersion.CSharp10)], []);

    // we don't need to test higher-level language versions: *every other test does that!*
}
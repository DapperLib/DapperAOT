using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP005 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ShouldFlagWhenUsedAndNotAttrib() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                conn.{|#0:Execute|}("somesql");
                conn.{|#1:Execute|}("somesql"); // to avoid clutter: only one diagnostic emitted (multiple locations)
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperAotNotEnabled).WithLocation(0).WithLocation(1).WithArguments(2)]);

    [Fact]
    public Task ShouldNotFlagWhenNotUsedAndNoAttrib() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        class SomeCode
        {
            public void Foo(DbConnection conn) {}
        }
        """, DefaultConfig, []);

    [Fact]
    public Task ShouldNotFlagWhenUsedAnywhere() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        class SomeCode
        {
            [DapperAot]
            public void Foo(DbConnection conn) => conn.Execute("somesql");

            public void Bar(DbConnection conn) => conn.Execute("somesql");
        }
        """, DefaultConfig, []);

    [Fact]
    public Task ShouldNotFlagWhenUsedAndOptedOut() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(false)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("somesql");
        }
        """, DefaultConfig, []);

    [Fact]
    public Task ShouldNotFlagWhenUsedAndOptedIn() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("somesql");
        }
        """, DefaultConfig, []);
}
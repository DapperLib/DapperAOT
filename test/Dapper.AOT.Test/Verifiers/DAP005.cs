﻿using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP005 : Verifier<WrappedDapperInterceptorAnalyzer>
{
    [Fact]
    public Task ShouldFlagWhenUsedAndNotAttrib() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("some sql");
        }
        """, DefaultConfig, [Diagnostic(DapperInterceptorGenerator.Diagnostics.DapperAotNotEnabled)]);

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
    public Task ShouldNotFlagWhenUsedAndOptedOut() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(false)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("some sql");
        }
        """, DefaultConfig, []);

    [Fact]
    public Task ShouldNotFlagWhenUsedAndOptedIn() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn) => conn.Execute("some sql");
        }
        """, DefaultConfig, [InterceptorsGenerated(1, 1, 1, 0, 0)]);
}
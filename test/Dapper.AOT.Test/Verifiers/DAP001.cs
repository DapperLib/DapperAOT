using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP001 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SingleTypeQueryArity1Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int>("select 1");
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity3Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int>("select 1", (a,b) => a + b);
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity4Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int,int>("select 1", (a,b,c) => a + b + c);
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity5Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int,int,int>("select 1", (a,b,c,d) => a + b + c + d);
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity6Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int,int,int,int>("select 1", (a,b,c,d,e) => a + b + c + d + e);
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity7Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int,int,int,int,int>("select 1", (a,b,c,d,e,f) => a + b + c + d + e + f);
            }
        }
        """, DefaultConfig, []);

    [Fact]
    public Task MultiMapArity8Supported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int,int,int,int,int,int,int,int>("select 1", (a,b,c,d,e,f,g) => a + b + c + d + e + f + g);
            }
        }
        """, DefaultConfig, []);
}
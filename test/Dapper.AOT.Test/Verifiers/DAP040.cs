using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP040 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task FactoryMethodAmbiguous() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<NoFactoryMethods>("storedproc");
                _ = conn.Query<SingleImplicit>("storedproc");
                _ = conn.Query<MultipleImplicit>("storedproc");
            }
        }
        class NoFactoryMethods { public int Id {get;set;} }
        class SingleImplicit
        {
            public int A {get; private set;}
            public static SingleImplicit Create(int a) => new SingleImplicit { A = a };
        }
        class MultipleImplicit
        {
            public int A {get; private set;}
            public static MultipleImplicit {|#0:Create1|}(int a) => new MultipleImplicit { A = a };
            public static MultipleImplicit Create2(int a) => new MultipleImplicit { A = a };
        }
        """, 
        DefaultConfig,
        [
            Diagnostic(Diagnostics.FactoryMethodAmbiguous).WithLocation(0).WithArguments("MultipleImplicit"),
        ]
    );

}
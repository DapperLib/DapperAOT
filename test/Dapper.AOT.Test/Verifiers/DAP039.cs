using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP039 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task FactoryMethodMultipleExplicit() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<NoFactoryMethods>("storedproc");
                _ = conn.Query<SingleExplicit>("storedproc");
                _ = conn.Query<MultipleExplicit>("storedproc");
            }
        }
        class NoFactoryMethods { public int Id {get;set;} }
        class SingleExplicit
        {
            public int A {get; private set;}
            [ExplicitConstructor] public static SingleExplicit Create1(int a) => new SingleExplicit { A = a };
            public static SingleExplicit Create2(int a) => new SingleExplicit { A = a };
        }
        class MultipleExplicit
        {
            public int A {get; private set;}
            [ExplicitConstructor] public static MultipleExplicit {|#0:Create1|}(int a) => new MultipleExplicit { A = a };
            [ExplicitConstructor] public static MultipleExplicit Create2(int a) => new MultipleExplicit { A = a };
            public static MultipleExplicit Create3(int a) => new MultipleExplicit { A = a };
        }
        """, 
        DefaultConfig,
        [
            Diagnostic(Diagnostics.FactoryMethodMultipleExplicit).WithLocation(0).WithArguments("MultipleExplicit"),
        ]
    );

}
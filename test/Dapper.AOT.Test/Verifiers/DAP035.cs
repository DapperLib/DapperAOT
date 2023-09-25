using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP035 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ConstructorMultipleExplicit() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<NoConstructors>("storedproc");
                _ = conn.Query<SingleExplicit>("storedproc");
                _ = conn.Query<MultipleExplicit>("storedproc");
            }
        }
        class NoConstructors { public int Id {get;set;} }
        class SingleExplicit
        {
            public int A {get;}
            [ExplicitConstructor] public SingleExplicit(int a) => A = a;
            public SingleExplicit(string b) {}
            public SingleExplicit(decimal c) {}
            public SingleExplicit(SingleExplicit d) {}
        }
        class MultipleExplicit
        {
            [ExplicitConstructor] public {|#0:MultipleExplicit|}(int a) {}
            [ExplicitConstructor] public MultipleExplicit(string b) {}
            public MultipleExplicit(decimal c) {}
            public MultipleExplicit(MultipleExplicit d) {}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ConstructorMultipleExplicit).WithLocation(0).WithArguments("MultipleExplicit"),
    ]);

}
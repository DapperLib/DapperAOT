using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP041 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ConstructorOverridesFactoryMethod() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<MultipleConstructionVariants>("storedproc");
            }
        }
        class MultipleConstructionVariants
        {
            public int A {get; private set;}
            [ExplicitConstructor] public MultipleConstructionVariants(int a) { A = a; }
            [ExplicitConstructor] public static MultipleConstructionVariants {|#0:Create|}(int a) => new MultipleConstructionVariants(a);
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.ConstructorOverridesFactoryMethod).WithLocation(0).WithArguments("MultipleConstructionVariants"),
        ]
    );

}
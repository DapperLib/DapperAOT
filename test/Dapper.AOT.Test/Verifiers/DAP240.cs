using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP240 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableParameterConflict() => CSVerifyAsync(""""
        using Dapper;
        using System.Data;
        using System.Data.Common;
        
        [DapperAot]
        class HasUnusedParameter
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("""
                declare {|#0:@a|} int = 42;
                declare @c2 nvarchar(200) = 'abc';

                insert SomeTable(Id, Name, Age)
                values (@a, @c2, @d);
                """, this);

            public int A {get;set;}
            public int B {get;set;}
            public int C {get;set;}
            public int D {get;set;}
        }
        """", DefaultConfig, [
        Diagnostic(Diagnostics.VariableParameterConflict).WithLocation(0).WithArguments("@a"),
        ]);

}
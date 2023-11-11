using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP236 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UnusedParameter() => CSVerifyAsync(""""
        using Dapper;
        using System.Data;
        using System.Data.Common;
        
        [DapperAot]
        class HasUnusedParameter
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute({|#0:"""
                insert SomeTable(Id, Name)
                values (@a, '@b'); -- @c
                """|}, this);

            public int A {get;set;} // used
            public int B {get;set;} // mentioned in string literal
            public int C {get;set;} // mentioned in comment
            public int D {get;set;} // not used - do NOT expect warning
        }
        """", DefaultConfig, [
        Diagnostic(Diagnostics.UnusedParameter).WithLocation(0).WithArguments("B"),
        Diagnostic(Diagnostics.UnusedParameter).WithLocation(0).WithArguments("C"),
        ]);

}
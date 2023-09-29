using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP217 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InsertColumnsMismatch() => SqlVerifyAsync("""
        insert SomeTable (A, B)
        {|#0:values (1, 2, 3)|}

        insert SomeTable (A, B)
        values (4,5)
        """, Diagnostic(Diagnostics.InsertColumnsMismatch).WithLocation(0));
    
}
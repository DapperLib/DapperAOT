using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP218 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InsertColumnsUnbalanced() => SqlVerifyAsync("""
        insert SomeTable (A, B)
        {|#0:values (1, 2), (3, 4, 5)|}

        insert SomeTable (A, B)
        values (6, 7), (8, 9)
        """, Diagnostic(Diagnostics.InsertColumnsUnbalanced).WithLocation(0));
    
}
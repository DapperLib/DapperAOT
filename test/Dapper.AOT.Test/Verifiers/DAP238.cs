using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP238 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task TrivialOperand() => SqlVerifyAsync("""
       select Age + {|#0:0|}
       from SomeTable
       """, [
        Diagnostic(Diagnostics.TrivialOperand).WithLocation(0),
        ]);

}
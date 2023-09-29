using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP237 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DivideByZero() => SqlVerifyAsync("""
       select Age / {|#0:(42 - 42)|}
       from SomeTable
       """, [
        Diagnostic(Diagnostics.DivideByZero).WithLocation(0),
        ]);

}
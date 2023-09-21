using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP213 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableValueNotConsumed() => SqlVerifyAsync("""
        declare @i int = 42;
        set {|#0:@i|} = 1;
        select @i;
        set {|#1:@i|} = 43;
        """, Diagnostic(Diagnostics.VariableValueNotConsumed).WithLocation(0).WithArguments("@i"),
            Diagnostic(Diagnostics.VariableValueNotConsumed).WithLocation(1).WithArguments("@i"));
    
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP211 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableAccessedBeforeDeclaration() => SqlVerifyAsync("""
        select {|#0:@i|};
        declare @i int;
        """, Diagnostic(Diagnostics.VariableAccessedBeforeDeclaration).WithLocation(0).WithArguments("@i"));
    
}
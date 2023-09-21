using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP214 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableNotDeclared() => SqlVerifyAsync("""
        select {|#0:@i|};
        """, SqlAnalysis.SqlParseInputFlags.KnownParameters, Diagnostic(Diagnostics.VariableNotDeclared).WithLocation(0).WithArguments("@i"));
    
}
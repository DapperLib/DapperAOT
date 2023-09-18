using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.SqlAnalysis.TSqlProcessor;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP214 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VariableNotDeclared() => SqlVerifyAsync("""
        select {|#0:@i|};
        """, ModeFlags.KnownParameters, Diagnostic(Diagnostics.VariableNotDeclared).WithLocation(0).WithArguments("@i"));
    
}
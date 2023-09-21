using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP210 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task TableVariableAccessedBeforePopulate() => SqlVerifyAsync("""
        declare @i int;
        select {|#0:@i|};
        set @i = 42;
        select @i;
        """, Diagnostic(Diagnostics.VariableAccessedBeforeAssignment).WithLocation(0).WithArguments("@i"));
    
}
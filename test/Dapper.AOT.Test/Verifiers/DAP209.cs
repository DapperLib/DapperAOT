using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP209 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task TableVariableAccessedBeforePopulate() => SqlVerifyAsync("""
        declare @t table (Value int not null);
        select Value from {|#0:@t|};
        insert @t (Value) values (1);
        select Value from @t;
        """, Diagnostic(Diagnostics.TableVariableAccessedBeforePopulate).WithLocation(0).WithArguments("@t"));
    
}
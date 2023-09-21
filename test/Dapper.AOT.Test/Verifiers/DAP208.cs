using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP208 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task TableVariableUsedAsScalar() => SqlVerifyAsync("""
        declare @id int = 0;
        declare @t table (Value int not null);
        insert @t (Value) values (1);
        select @id;
        select {|#0:@t|};
        """, Diagnostic(Diagnostics.TableVariableUsedAsScalar).WithLocation(0).WithArguments("@t"));
    
}
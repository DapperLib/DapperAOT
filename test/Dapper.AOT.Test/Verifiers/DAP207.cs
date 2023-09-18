using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP207 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ScalarVariableUsedAsTable() => SqlVerifyAsync("""
        declare @id int = 0;
        declare @t table (Value int not null);
        insert @t (Value) values (1);
        select Value from {|#0:@id|};
        select Value from @t;
        """, Diagnostic(Diagnostics.ScalarVariableUsedAsTable).WithLocation(0).WithArguments("@id"));
    
}
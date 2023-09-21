using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP232 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NonPositiveFetch() => SqlVerifyAsync("""
        select Id, Name
        from Users
        order by Name
        offset 0 rows
        fetch next {|#0:0|} row only

        select Id, Name
        from Users
        order by Name
        offset 0 rows
        fetch next 1 row only
        """, Diagnostic(Diagnostics.NonPositiveFetch).WithLocation(0));
    
}
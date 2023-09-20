using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP235 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task TopWithOffset() => SqlVerifyAsync("""
        select {|#0:top 10|} Id, Name
        from Users
        order by Name
        offset 0 rows

        select Id, Name
        from Users
        order by Name
        offset 0 rows
        fetch next 10 rows only
        """,
        Diagnostic(Diagnostics.TopWithOffset).WithLocation(0)
        );
    
}
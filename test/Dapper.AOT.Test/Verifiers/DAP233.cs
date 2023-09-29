using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP233 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NegativeOffset() => SqlVerifyAsync("""
        select Id, Name
        from Users
        order by Name
        offset {|#0:-1|} rows
        fetch next 1 row only

        select Id, Name
        from Users
        order by Name
        offset 0 rows
        fetch next 1 row only
        """, Diagnostic(Diagnostics.NegativeOffset).WithLocation(0));
    
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP223 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DeleteWithoutWhere() => SqlVerifyAsync("""
        {|#0:delete from Users|}

        delete from Users where Region='North'

        delete u
        from Users u
        inner join Foo x on x.Id = u.Id
        """, Diagnostic(Diagnostics.DeleteWithoutWhere).WithLocation(0));
    
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP224 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UpdateWithoutWhere() => SqlVerifyAsync("""
        {|#0:update Users set Balance = 0|}

        update Users set Balance = 0 where Region='North'

        update u
        set u.Balance = 0
        from Users u
        inner join Foo x on x.Id = u.Id
        """, Diagnostic(Diagnostics.UpdateWithoutWhere).WithLocation(0));

}
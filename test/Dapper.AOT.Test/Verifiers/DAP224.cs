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
        """, Diagnostic(Diagnostics.UpdateWithoutWhere).WithLocation(0));
    
}
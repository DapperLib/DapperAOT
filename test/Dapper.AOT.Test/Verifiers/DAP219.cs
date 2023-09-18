using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP219 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectStar() => SqlVerifyAsync("""
        select {|#0:*|} from Users

        select Id, Name from Users
        """, Diagnostic(Diagnostics.SelectStar).WithLocation(0));
    
}
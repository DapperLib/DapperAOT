using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP227 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NonIntegerTop() => SqlVerifyAsync("""
        select top {|#0:22.5|} Id, Name from Users

        select top 22.5 percent Id, Name from Users
        
        select top 22 Id, Name from Users
        """, Diagnostic(Diagnostics.NonIntegerTop).WithLocation(0));
    
}
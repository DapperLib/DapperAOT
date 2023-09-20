using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP228 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NonPositiveTop() => SqlVerifyAsync("""
        select top {|#0:0|} Id, Name from Users

        select top {|#1:0|} percent Id, Name from Users
        
        select top 1 Id, Name from Users
        select top 1 percent Id, Name from Users
        """, Diagnostic(Diagnostics.NonPositiveTop).WithLocation(0), Diagnostic(Diagnostics.NonPositiveTop).WithLocation(1));
    
}
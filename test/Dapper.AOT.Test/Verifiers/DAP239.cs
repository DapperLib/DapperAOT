using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP239 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InvalidNullExpression() => SqlVerifyAsync("""
       select 42 >> {|#0:null|}

       select {|#1:~null|} >> 42

       select {|#2:null & null|}
       """, [
        Diagnostic(Diagnostics.InvalidNullExpression).WithLocation(0),
        Diagnostic(Diagnostics.InvalidNullExpression).WithLocation(1),
        Diagnostic(Diagnostics.InvalidNullExpression).WithLocation(2),
        ]);

}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP234 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SimplifyExpression() => SqlVerifyAsync("""
        select {|#0:1 + (2 * 5)|} as [Score], -1 as [Allowed], {|#1:+1|} as [NotAllowed]

        select {|#2:1 + 10|} as [Score]

        select 11 as [Score], (11) as [Meh]

        select Age / -1
        from SomeTable

        select {|#3:1 / null|} as [oops]
        declare @i int = 42;
        select {|#4:15 / @i + null|}

        select {|#5:1 + 10.0|} as [Score]
        """,
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(0).WithArguments("11"),
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(1).WithArguments("1"),
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(2).WithArguments("11"),
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(3).WithArguments("null"),
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(4).WithArguments("null"),
        Diagnostic(Diagnostics.SimplifyExpression).WithLocation(5).WithArguments("11.0")
        );
    
}
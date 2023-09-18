using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP205 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NullLiteralComparison() => SqlVerifyAsync("""
        select A, B
        from SomeTable
        where X = {|#0:null|} and Y != {|#1:null|} and Z > {|#2:null|}
        """,
        Diagnostic(Diagnostics.NullLiteralComparison).WithLocation(0),
        Diagnostic(Diagnostics.NullLiteralComparison).WithLocation(1),
        Diagnostic(Diagnostics.NullLiteralComparison).WithLocation(2));

    [Fact]
    public Task CompareWithAnsiNulls() => SqlVerifyAsync("""
        select A, B
        from SomeTable
        where X is null and Y is not null
        """);
}
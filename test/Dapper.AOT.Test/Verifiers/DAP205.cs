using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP205 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task CompareToNull() => SqlVerifyAsync("""
        select A, B
        from SomeTable
        where X = {|#0:null|} and Y != {|#1:null|} and Z > {|#2:null|}
        """,
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(0),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(1),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(2));

    [Fact]
    public Task CompareWithAnsiNulls() => SqlVerifyAsync("""
        select A, B
        from SomeTable
        where X is null and Y is not null
        """);
}
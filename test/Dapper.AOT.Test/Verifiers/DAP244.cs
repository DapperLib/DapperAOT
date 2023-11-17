using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP244 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValidNonAggregate() => SqlVerifyAsync("""
        select Name
        from SomeTable
        """);

    [Fact]
    public Task ValidAggregate() => SqlVerifyAsync("""
        select COUNT(1), COUNT(DISTINCT Name)
        from SomeTable
        """);

    [Fact]
    public Task InvalidMix() => SqlVerifyAsync("""
        {|#0:select COUNT(1), COUNT(DISTINCT Name), Name
        from SomeTable|}
        """, Diagnostic(Diagnostics.SelectAggregateMismatch).WithLocation(0));

}
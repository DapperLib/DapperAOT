using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP243 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task Valid() => SqlVerifyAsync("""
        SELECT DATEPART(Year, GETUTCDATE())
        """);

    [Fact]
    public Task UnknownValue() => SqlVerifyAsync("""
        SELECT DATEPART({|#0:NotValid|}, GETUTCDATE())
        """, Diagnostic(Diagnostics.InvalidDatepartToken).WithLocation(0));

    [Fact]
    public Task Parameter() => SqlVerifyAsync("""
        SELECT DATEPART({|#0:@Foo|}, GETUTCDATE())
        """, Diagnostic(Diagnostics.InvalidDatepartToken).WithLocation(0));

    [Fact]
    public Task Literal() => SqlVerifyAsync("""
        SELECT DATEPART({|#0:42|}, GETUTCDATE())
        """, Diagnostic(Diagnostics.InvalidDatepartToken).WithLocation(0));

}
using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP230 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectSingleTopError() => SqlVerifyAsync("""
        select top {|#0:1|} Id, Name from Users where Id=42
        """, SqlParseInputFlags.SingleRow | SqlParseInputFlags.AtMostOne, Diagnostic(Diagnostics.SelectSingleTopError).WithLocation(0));

    [Fact]
    public Task SelectSingleTopError_Fetch() => SqlVerifyAsync("""
        select Id, Name from Users where Id=42 order by Name offset 0 rows fetch next {|#0:1|} row only
        """, SqlParseInputFlags.SingleRow | SqlParseInputFlags.AtMostOne, Diagnostic(Diagnostics.SelectSingleTopError).WithLocation(0));

    [Fact]
    public Task SelectSingleTopError_TooHigh() => SqlVerifyAsync("""
        select top {|#0:3|} Id, Name from Users where Id=42
        """, SqlParseInputFlags.SingleRow | SqlParseInputFlags.AtMostOne, Diagnostic(Diagnostics.SelectSingleTopError).WithLocation(0));

    [Fact]
    public Task SelectSingleTopError_Pass() => SqlVerifyAsync("""
        select top 2 Id, Name from Users where Id=42
        """, SqlParseInputFlags.SingleRow | SqlParseInputFlags.AtMostOne);

}
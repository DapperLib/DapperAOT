using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP229 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectFirstTopError() => SqlVerifyAsync("""
        select top {|#0:2|} Id, Name from Users where Id=42
        """, SqlAnalysis.SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectFirstTopError).WithLocation(0));

    [Fact]
    public Task SelectFirstTopError_Pass() => SqlVerifyAsync("""
        select top 1 Id, Name from Users where Id=42
        """, SqlAnalysis.SqlParseInputFlags.SingleRow);

}
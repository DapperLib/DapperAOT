using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.SqlAnalysis.TSqlProcessor;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP229 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectFirstTopError() => SqlVerifyAsync("""
        select {|#0:top 2|} Id, Name from Users where Id=42
        """, ModeFlags.SingleRow, Diagnostic(Diagnostics.SelectFirstTopError).WithLocation(0));

    [Fact]
    public Task SelectFirstTopError_Pass() => SqlVerifyAsync("""
        select top 1 Id, Name from Users where Id=42
        """, ModeFlags.SingleRow);

}
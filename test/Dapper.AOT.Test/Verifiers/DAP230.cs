using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.SqlAnalysis.TSqlProcessor;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP230 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectSingleTopError() => SqlVerifyAsync("""
        select {|#0:top 1|} Id, Name from Users where Id=42
        """, ModeFlags.SingleRow | ModeFlags.AtMostOne, Diagnostic(Diagnostics.SelectSingleTopError).WithLocation(0));

    [Fact]
    public Task SelectSingleTopError_TooHigh() => SqlVerifyAsync("""
        select {|#0:top 3|} Id, Name from Users where Id=42
        """, ModeFlags.SingleRow | ModeFlags.AtMostOne, Diagnostic(Diagnostics.SelectSingleTopError).WithLocation(0));

    [Fact]
    public Task SelectSingleTopError_Pass() => SqlVerifyAsync("""
        select top 2 Id, Name from Users where Id=42
        """, ModeFlags.SingleRow | ModeFlags.AtMostOne);

}
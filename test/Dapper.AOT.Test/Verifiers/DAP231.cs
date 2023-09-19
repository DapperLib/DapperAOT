using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.SqlAnalysis.TSqlProcessor;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP231 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectSingleRowWithoutWhere() => SqlVerifyAsync("""
        {|#0:select Id, Name from Users|}
        """, ModeFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithWhere() => SqlVerifyAsync("""
        select Id, Name from Users where Id=42
        """, ModeFlags.SingleRow);

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithTopAndOrderBy() => SqlVerifyAsync("""
        select top 1 Id, Name from Users order by Name
        """, ModeFlags.SingleRow);

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithTopNoOrderBy() => SqlVerifyAsync("""
        {|#0:select top 1 Id, Name from Users|}
        """, ModeFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithOrderByNoTop() => SqlVerifyAsync("""
        {|#0:select Id, Name from Users order by Name|}
        """, ModeFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutFrom() => SqlVerifyAsync("""
        select HasRecords = case when exists (select top 1 1 from MyTable) then 1 else 0 end
        """, ModeFlags.SingleRow);

}
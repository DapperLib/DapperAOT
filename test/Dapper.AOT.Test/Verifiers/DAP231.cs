using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP231 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectSingleRowWithoutWhere() => SqlVerifyAsync("""
        {|#0:select Id, Name from Users|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithWhere() => SqlVerifyAsync("""
        select Id, Name from Users where Id=42
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithTopAndOrderBy() => SqlVerifyAsync("""
        select top 1 Id, Name from Users order by Name
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithTopNoOrderBy() => SqlVerifyAsync("""
        {|#0:select top 1 Id, Name from Users|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutWhere_WithOrderByNoTop() => SqlVerifyAsync("""
        {|#0:select Id, Name from Users order by Name|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact]
    public Task SelectSingleRowWithoutFrom() => SqlVerifyAsync("""
        select HasRecords = case when exists (select top 1 1 from MyTable) then 1 else 0 end
        """, SqlParseInputFlags.SingleRow);

    [Fact] // false positive https://github.com/DapperLib/DapperAOT/issues/81
    public Task DoNotReportForAggregate() => SqlVerifyAsync("""
        SELECT MAX(SomeColumn) FROM MyTable
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task DoNotReportForMultipleAggregatesAndExpressions() => SqlVerifyAsync("""
        SELECT -MAX(SomeColumn), COUNT(1) + 32 FROM MyTable
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task DoNotReportForWrappedAggregate() => SqlVerifyAsync("""
        SELECT ISNULL(SUM(SomeColumn), 0) FROM MyTable
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task DoNotReportForNewerAggregate() => SqlVerifyAsync("""
        SELECT MEDIAN(SomeColumn) FROM MyTable
        """, SqlParseInputFlags.SingleRow);

    [Fact]
    public Task DoNotReportForUserFunctionOverAggregate() => SqlVerifyAsync("""
        SELECT dbo.FormatMoney(SUM(SomeColumn)) FROM MyTable
        """, SqlParseInputFlags.SingleRow);

    [Fact] // dbo.Median is a user function, not the MEDIAN aggregate
    public Task ReportForUserFunctionNamedLikeAggregate() => SqlVerifyAsync("""
        {|#0:SELECT dbo.Median(SomeColumn) FROM MyTable|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact] // a windowed aggregate returns one row per source row
    public Task ReportForWindowedAggregate() => SqlVerifyAsync("""
        {|#0:SELECT COUNT(1) OVER () FROM MyTable|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

    [Fact] // the subquery runs once per row of MyTable
    public Task ReportForSubqueryColumn() => SqlVerifyAsync("""
        {|#0:SELECT (SELECT COUNT(1) FROM OtherTable) FROM MyTable|}
        """, SqlParseInputFlags.SingleRow, Diagnostic(Diagnostics.SelectSingleRowWithoutWhere).WithLocation(0));

}
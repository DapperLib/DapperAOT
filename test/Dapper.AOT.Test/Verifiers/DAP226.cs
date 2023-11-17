using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP226 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task FromMultiTableUnqualifiedColumn() => SqlVerifyAsync("""
        select a.Id, {|#0:Name|}
        from A a
        inner join B b on b.X = a.Id

        select a.Id, b.Name
        from A a
        inner join B b on b.X = a.Id
        """, Diagnostic(Diagnostics.FromMultiTableUnqualifiedColumn).WithLocation(0).WithArguments("Name"));

    [Fact] // false positive: https://github.com/DapperLib/DapperAOT/issues/80
    public Task DoNotReportDateAdd() => SqlVerifyAsync("""
        SELECT DATEADD(YEAR, 1, t.Year) AS NextYear
        FROM MyTable t
        JOIN MyOtherTable o ON o.Id = t.Id
        """);

}
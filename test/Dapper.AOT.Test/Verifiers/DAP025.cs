using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP025 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task QueryCommandMissingQuery() => CSVerifyAsync(""""
        using Dapper;
        using System.Data.Common;
        using System.Linq;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                // no query
                var id = conn.{|#0:Execute|}("""
                    select Id, Name
                    from SomeLookupTable
                    """);

                // fine if we don't have a query
                id = conn.Execute("""
                    insert SomeLookupTable (Name)
                    values ('North')
                    """);

                // fine if we use query
                conn.Query("""
                    select Id, Name
                    from SomeLookupTable
                    """);

                // fine if it looks like an SP call
                conn.Execute("""
                    exec SomeLookupFetch
                """);
            }
        }
        """", DefaultConfig, [
            Diagnostic(Diagnostics.ExecuteCommandWithQuery).WithLocation(0),
        ]);

    [Fact]
    public Task ReportWhenNoQueryExpected() => SqlVerifyAsync("""
        SELECT [Test]
        FROM MyTable
        """, SqlAnalysis.SqlParseInputFlags.ExpectNoQuery,
    Diagnostic(Diagnostics.ExecuteCommandWithQuery).WithLocation(Execute));

    [Fact] // false positive scenario, https://github.com/DapperLib/DapperAOT/issues/79
    public Task DoNotReportSelectInto() => SqlVerifyAsync("""
        SELECT [Test]
        INTO #MyTemporaryTable FROM MyTable
        """, SqlAnalysis.SqlParseInputFlags.ExpectNoQuery);

}
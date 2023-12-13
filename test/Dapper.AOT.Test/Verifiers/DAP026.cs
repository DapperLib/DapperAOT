using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP026 : Verifier<DapperAnalyzer>
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
                var id = conn.{|#0:QuerySingle<int>|}("""
                    insert SomeLookupTable (Name)
                    values ('North')
                    """);

                // fine if we have a query
                id = conn.QuerySingle<int>("""
                    insert SomeLookupTable (Name)
                    output inserted.Id
                    values ('North')
                    """);

                // fine if we use execute
                conn.Execute("""
                    insert SomeLookupTable (Name)
                    values ('North')
                    """);

                // fine if it looks like an SP call
                conn.Execute("""
                    exec SomeLookupInsert 'North'
                """);
            }
        }
        """", DefaultConfig, [
            Diagnostic(Diagnostics.QueryCommandMissingQuery).WithLocation(0),
        ]);

    [Fact]
    public Task DoNotReportWhenQueryExpected() => SqlVerifyAsync("""
        SELECT [Test]
        FROM MyTable
        """, SqlAnalysis.SqlParseInputFlags.ExpectQuery);

    [Fact]
    public Task ReportSelectInto() => SqlVerifyAsync("""
        SELECT [Test]
        INTO #MyTemporaryTable FROM MyTable
        """, SqlAnalysis.SqlParseInputFlags.ExpectQuery,
            Diagnostic(Diagnostics.QueryCommandMissingQuery).WithLocation(Execute));

}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP018(ITestOutputHelper log) : Verifier<DapperAnalyzer>(log)
{
    [Fact]
    public Task SqlParametersNotDetected() => CSVerifyAsync(""""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeType
        {
            public void NoArgsOrLocals(DbConnection connection)
                => connection.Execute("""
                insert SomeTable(Id) values (42)
                """, {|#0:new { id = 42 }|});

            public void UsesArgs(DbConnection connection)
                => connection.Execute("""
                insert SomeTable(Id) values (@id)
                """, new { id = 42 });

            public void UsesThingsCloseToArgs(DbConnection connection)
                => connection.Execute("""
                declare @id2 int = 14;
                insert SomeTable(Id, Age) values (@@id, @id2)
                """, {|#1:new { id = 42 }|});
        }
        """", DefaultConfig, [
            Diagnostic(Diagnostics.SqlParametersNotDetected).WithLocation(0),
            Diagnostic(Diagnostics.SqlParametersNotDetected).WithLocation(1),
    ]);

    [Fact]
    public Task NoFalsePositive_Issue2203_Typed() => CSVerifyAsync(
        // https://github.com/DapperLib/Dapper/issues/2203
        """"
        using Dapper;
        using Microsoft.Data.SqlClient;

        [DapperAot]
        public static class MyType
        {
            public static int Count(SqlConnection conn, int cod)
                => conn.QuerySingle<int>(
                    """select count(*) from CAD.TBL_APLICACAO APP where (APP.APP_COD = @APP_COD)""",
                    new { APP_COD = cod });
        }
        """", DefaultConfig, []);
    
    [Fact]
    public Task NoFalsePositive_Issue2203_Dynamic() => CSVerifyAsync(
        // https://github.com/DapperLib/Dapper/issues/2203
        """"
        using Dapper;
        using System.Data;
        using Microsoft.Data.SqlClient;

        [DapperAot(false)] // don't need to know about DAP015
        public static class MyType
        {
            public static int Count(SqlConnection conn, int cod)
            {
                var parameters = new DynamicParameters();
                parameters.Add("APP_COD", cod, DbType.Int64);

                return conn.QuerySingle<int>(
                    """select count(*) from CAD.TBL_APLICACAO APP where (APP.APP_COD = @APP_COD)""",
                    parameters);
            }
        }
        """", DefaultConfig, []);
}
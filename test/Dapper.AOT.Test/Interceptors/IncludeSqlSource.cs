using Dapper;
using System.Data.SqlClient;

[module: DapperAot]
[module: IncludeSqlSourceAttribute]
public static class Foo
{
    static void SystemData(SqlConnection db)
    {
        db.Execute("print GETDATE();");
    }
}
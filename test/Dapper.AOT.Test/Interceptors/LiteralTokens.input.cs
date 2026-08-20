using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class LiteralTokens
{
    public static void Query(DbConnection connection)
        => connection.QuerySingle<int>(
            "select Id from Users where UserTypeId = {=Admin} and A = @a",
            new { Admin = 1, a = 2 });
}

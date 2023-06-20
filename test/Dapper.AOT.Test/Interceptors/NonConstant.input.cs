using Dapper;
using System;
using System.Data;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        var obj = new { A = 1, B = 2, C = 3 };
        var arr = new[] { obj };

        var sql = GetSql(out var type);

        connection.Execute(sql, obj, commandType: type);

        connection.Execute(sql, arr, commandType: type);
    }

    static string GetSql(out CommandType type) => throw new NotImplementedException();
}
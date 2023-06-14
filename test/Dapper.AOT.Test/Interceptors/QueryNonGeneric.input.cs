using Dapper;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.Query("def", obj);
        _ = connection.Query("def", obj, buffered: isBuffered);
        _ = connection.Query("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query("def", obj, buffered: true, commandType: CommandType.Text);

        _ = await connection.QueryAsync("def");
        _ = await connection.QueryAsync("def", obj);
    }
}
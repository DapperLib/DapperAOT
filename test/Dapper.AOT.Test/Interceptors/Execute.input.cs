using Dapper;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.Execute("def", obj);
        _ = connection.Execute("def", commandType: CommandType.StoredProcedure);
        _ = connection.Execute("def", commandType: CommandType.Text);

        _ = await connection.ExecuteAsync("def", obj);
        _ = await connection.ExecuteAsync("def", commandType: CommandType.StoredProcedure);
        _ = await connection.ExecuteAsync("def", commandType: CommandType.Text);
    }
}
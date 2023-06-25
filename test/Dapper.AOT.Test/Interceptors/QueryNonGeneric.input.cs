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
        _ = connection.Query("def");
        _ = connection.Query("def", obj);
        _ = connection.Query("def", obj, buffered: isBuffered);
        _ = connection.Query("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query("def @Foo", obj, buffered: true, commandType: CommandType.Text);

        _ = await connection.QueryAsync("def");
        _ = await connection.QueryAsync("def", obj);
#if !NETFRAMEWORK
        await foreach (var item in connection.QueryUnbufferedAsync("def", commandType: CommandType.StoredProcedure)) { }
        await foreach (var item in connection.QueryUnbufferedAsync("def @Foo", obj, commandType: CommandType.Text)) { }
#endif

        _ = connection.QueryFirst("def", obj);
        _ = connection.QueryFirstOrDefault("def", obj, commandType: CommandType.StoredProcedure);
        _ = connection.QuerySingle("def @foo", obj, commandType: CommandType.Text);
        _ = connection.QuerySingleOrDefault("def");

        _ = await connection.QueryFirstAsync("def", obj);
        _ = await connection.QueryFirstOrDefaultAsync("def", obj, commandType: CommandType.StoredProcedure);
        _ = await connection.QuerySingleAsync("def @foo", obj, commandType: CommandType.Text);
        _ = await connection.QuerySingleOrDefaultAsync("def");
    }
}
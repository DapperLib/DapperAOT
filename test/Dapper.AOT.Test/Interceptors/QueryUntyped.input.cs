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
        _ = connection.Query<object>("def");
        _ = connection.Query<object>("def", obj);
        _ = connection.Query<object>("def", obj, buffered: isBuffered);
        _ = connection.Query<object>("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query<object>("def @Foo", obj, buffered: true, commandType: CommandType.Text);

        _ = connection.Query<dynamic>("def");
        _ = connection.Query<dynamic>("def", obj);
        _ = connection.Query<dynamic>("def", obj, buffered: isBuffered);
        _ = connection.Query<dynamic>("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query<dynamic>("def @Foo", obj, buffered: true, commandType: CommandType.Text);

        _ = connection.Query("def");
        _ = connection.Query("def", obj);
        _ = connection.Query("def", obj, buffered: isBuffered);
        _ = connection.Query("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query("def @Foo", obj, buffered: true, commandType: CommandType.Text);

        _ = await connection.QueryAsync<object>("def");
        _ = await connection.QueryAsync<object>("def", obj);
#if !NETFRAMEWORK
        await foreach (var item in connection.QueryUnbufferedAsync<object>("def", commandType: CommandType.StoredProcedure)) { }
        await foreach (var item in connection.QueryUnbufferedAsync<object>("def @Foo", obj, commandType: CommandType.Text)) { }
#endif
        _ = await connection.QueryAsync<dynamic>("def");
        _ = await connection.QueryAsync<dynamic>("def", obj);
#if !NETFRAMEWORK
        await foreach (var item in connection.QueryUnbufferedAsync<dynamic>("def", commandType: CommandType.StoredProcedure)) { }
        await foreach (var item in connection.QueryUnbufferedAsync<dynamic>("def @Foo", obj, commandType: CommandType.Text)) { }
#endif

        _ = await connection.QueryAsync("def");
        _ = await connection.QueryAsync("def", obj);
    }
}
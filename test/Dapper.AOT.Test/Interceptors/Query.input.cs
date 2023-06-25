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
        _ = connection.Query<Customer>("def");
        _ = connection.Query<Customer>("def", obj);
        _ = connection.Query<Customer>("def", obj, buffered: isBuffered);
        _ = connection.Query<Customer>("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query<Customer>("def @Foo", obj, buffered: true, commandType: CommandType.Text);

        _ = await connection.QueryAsync<Customer>("def");
        _ = await connection.QueryAsync<Customer>("def", obj);
#if !NETFRAMEWORK
        await foreach (var item in connection.QueryUnbufferedAsync<Customer>("def", commandType: CommandType.StoredProcedure)) { }
        await foreach (var item in connection.QueryUnbufferedAsync<Customer>("def @Foo", obj, commandType: CommandType.Text)) { }
#endif
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
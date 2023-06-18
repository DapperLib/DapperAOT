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
        _ = connection.QueryFirst<Customer>("def", obj);
        _ = connection.QueryFirstOrDefault<Customer>("def", obj, commandType: CommandType.StoredProcedure);
        _ = connection.QuerySingle<Customer>("def @foo", obj, commandType: CommandType.Text);
        _ = connection.QuerySingleOrDefault<Customer>("def");

        _ = await connection.QueryFirstAsync<Customer>("def", obj);
        _ = await connection.QueryFirstOrDefaultAsync<Customer>("def", obj, commandType: CommandType.StoredProcedure);
        _ = await connection.QuerySingleAsync<Customer>("def @foo", obj, commandType: CommandType.Text);
        _ = await connection.QuerySingleOrDefaultAsync<Customer>("def");
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
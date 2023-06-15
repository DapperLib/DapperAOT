using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.Query<Customer, Customer, Customer>("def", null);
        _ = connection.Query<Customer, Customer, Customer, Customer>("def", null);
        _ = connection.Query<Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = connection.Query<Customer, Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = connection.Query<Customer, Customer, Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = connection.Query<Customer, Customer, Customer, Customer, Customer, Customer, Customer, Customer>("def", null);

        _ = await connection.QueryAsync<Customer, Customer, Customer>("def", null);
        _ = await connection.QueryAsync<Customer, Customer, Customer, Customer>("def", null);
        _ = await connection.QueryAsync<Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = await connection.QueryAsync<Customer, Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = await connection.QueryAsync<Customer, Customer, Customer, Customer, Customer, Customer, Customer>("def", null);
        _ = await connection.QueryAsync<Customer, Customer, Customer, Customer, Customer, Customer, Customer, Customer>("def", null);
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
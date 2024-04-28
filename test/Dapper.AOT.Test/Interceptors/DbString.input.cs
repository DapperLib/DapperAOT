using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection)
    {
        _ = await connection.QueryAsync<int>("select * from Orders where name = @name", new
        {
            name = new DbString
            {
                Value = "myOrder",
                IsFixedLength = false,
                Length = 7,
                IsAnsi = true
            }
        });

    }

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
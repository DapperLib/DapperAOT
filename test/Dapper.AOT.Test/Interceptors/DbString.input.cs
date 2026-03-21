using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection)
    {
        _ = await connection.QueryAsync<Product>("select * from Orders where name = @Name and id = @Id", new
        {
            Name = new DbString
            {
                Value = "myOrder",
                IsFixedLength = false,
                Length = 7,
                IsAnsi = true
            },

            Id = 123
        });

        _ = await connection.QueryAsync<Product>("select * from Orders where name = @Name and id = @Id", new QueryModel
        {
            Name = new DbString
            {
                Value = "myOrder",
                IsFixedLength = false,
                Length = 7,
                IsAnsi = true
            },

            Id = 123
        });
    }

    public class QueryModel
    {
        public DbString Name { get; set; }
        public int Id { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string ProductNumber { get; set; }
    }
}
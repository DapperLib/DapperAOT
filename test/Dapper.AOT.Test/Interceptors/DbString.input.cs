using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection)
    {
        _ = await connection.QueryAsync<int>("select * from Orders where name = @Name and id = @Id", new
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

        _ = await connection.QueryAsync<int>("select * from Orders where name = @Name and id = @Id", new Poco
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

    public class Poco
    {
        public DbString Name { get; set; }
        public int Id { get; set; }
    }
}
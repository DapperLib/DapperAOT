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

        //_ = await connection.QueryAsync<int>("select * from Orders where name = @name", new Poco
        //{
        //    Name = new DbString
        //    {
        //        Value = "myOrder",
        //        IsFixedLength = false,
        //        Length = 7,
        //        IsAnsi = true
        //    }
        //});
    }

    public class Poco
    {
        public DbString Name { get; set; }
    }
}
using System;
using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection)
    {
        _ = await connection.QueryAsync<User>("select * from users where birth_date = @BirthDate", new
        {
            BirthDate = DateOnly.FromDateTime(DateTime.Now)
        });

        _ = await connection.QueryAsync<User>("select * from users where birth_date = @BirthDate", new QueryModel
        {
            BirthDate = DateOnly.FromDateTime(DateTime.Now)
        });
    }

    public class QueryModel
    {
        public DateOnly BirthDate { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateOnly BirthDate { get; set; }
    }
}
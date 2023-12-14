using Dapper;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, int id, CancellationToken cancellation)
    {
        _ = await connection.QueryAsync<MyType>("some sql", cancellation);
        _ = await connection.ExecuteAsync("some sql @id", new { id, cancellation });
    }

    public class MyType
    {
        public int Id { get; set; }
    }
}
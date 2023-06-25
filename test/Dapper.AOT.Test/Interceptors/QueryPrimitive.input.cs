using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection)
    {
        _ = connection.Query<int>("def");
        _ = connection.Query<int?>("def");
        _ = connection.Query<string>("def");
        _ = connection.QueryFirst<int>("def");
        _ = connection.QueryFirst<int?>("def");
        _ = connection.QueryFirst<string>("def");
        _ = connection.QueryFirstOrDefault<int>("def");
        _ = connection.QueryFirstOrDefault<int?>("def");
        _ = connection.QueryFirstOrDefault<string>("def");

        _ = await connection.QueryAsync<int>("def");
        _ = await connection.QueryAsync<int?>("def");
        _ = await connection.QueryAsync<string>("def");
#if !NETFRAMEWORK
        await foreach (var item in connection.QueryUnbufferedAsync<int>("def")) { }
        await foreach (var item in connection.QueryUnbufferedAsync<int?>("def")) { }
        await foreach (var item in connection.QueryUnbufferedAsync<string>("def")) { }
#endif
        _ = await connection.QueryFirstAsync<int>("def");
        _ = await connection.QueryFirstAsync<int?>("def");
        _ = await connection.QueryFirstAsync<string>("def");
        _ = await connection.QueryFirstOrDefaultAsync<int>("def");
        _ = await connection.QueryFirstOrDefaultAsync<int?>("def");
        _ = await connection.QueryFirstOrDefaultAsync<string>("def");



    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
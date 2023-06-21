using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

public static class Foo
{
    [DapperAot]
    static async Task SomeCodeExpectNotSupported(DbConnection connection, string bar, bool isBuffered)
    {
        var obj = new { Foo = 12, bar };
        using (var multi = connection.QueryMultiple("def", obj))
        {
            _ = multi.Read();
        }
#if !NETFRAMEWORK
        await
#endif
        using (var multi = await connection.QueryMultipleAsync("def", obj))
        {
            _ = await multi.ReadAsync();
        }
    }

    // no warning here because AOT not enabled
    static async Task SomeCodeExpectNoWarning(DbConnection connection, string bar, bool isBuffered)
    {
        var obj = new { Foo = 12, bar };
        using (var multi = connection.QueryMultiple("def", obj))
        {
            _ = multi.Read();
        }
#if !NETFRAMEWORK
        await
#endif
        using (var multi = await connection.QueryMultipleAsync("def", obj))
        {
            _ = await multi.ReadAsync();
        }
    }
}
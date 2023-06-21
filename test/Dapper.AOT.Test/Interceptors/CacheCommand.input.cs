using Dapper;
using System.Data.Common;
using System.Data;

[module: DapperAot]

public static class Foo
{
    [CacheCommand]
    static void SomeCode(DbConnection connection, string sql, string bar)
    {
        var obj = new { id = 12, bar };

        // these should have the same map, so should use different instances of the same abstract type
        _ = connection.Execute("insert Foo (Id, Value) values (@id, @bar)", obj);
        _ = connection.Execute("update Foo set Value = @bar where Id = @id", obj);

        // this has a different map, so should get a dedicated non-abstract instance
        _ = connection.Execute("update Foo set Enabled = 1 where Id = @id", obj);

        // this should not be cached
        _ = connection.Execute(sql, obj, commandType: CommandType.Text);
    }
}
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
        _ = connection.Execute("insert Foo (Id, Value) values (@id, @bar)", obj);

        _ = connection.Execute("update Foo set Value = @bar where Id = @id", obj);

        _ = connection.Execute(sql, obj, commandType: CommandType.Text);
    }
}
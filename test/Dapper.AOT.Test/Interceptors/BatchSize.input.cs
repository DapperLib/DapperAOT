using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    [BatchSize(10)] // should be passed explicitly
    static void SomeCode(DbConnection connection, string sql, string bar)
    {
        var objs = new[] { new { id = 12, bar }, new { id = 34, bar = "def" } };

        connection.Execute("insert Foo (Id, Value) values (@id, @bar)", objs);
    }

    // no batch size, should be passed implicitly
    static void SomeOtherCode(DbConnection connection, string sql, string bar)
    {
        var objs = new[] { new { id = 12, bar }, new { id = 34, bar = "def" } };

        connection.Execute("insert Foo (Id, Value) values (@id, @bar)", objs);
    }
}
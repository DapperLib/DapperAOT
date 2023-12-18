using Dapper;
using System.Data.Common;

[DapperAot]
public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var args = new { Foo = 12, bar };
        // these should support Id+Name
        var obj = connection.QueryFirst<Entity1>("def", args);
        connection.Execute("ghi @Id, @Name", obj);
    }
}

public abstract class EntityBase
{
    public long Id { get; set; }
}

public class Entity1 : EntityBase
{
    public string Name { get; set; }
}
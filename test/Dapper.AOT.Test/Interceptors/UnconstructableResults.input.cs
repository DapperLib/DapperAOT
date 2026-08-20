using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // like EF's DbGeography: no accessible parameterless constructor, but has a settable member
        _ = connection.Query<PrivateCtor>("select_sql");
        // like System.Data.Linq.Binary: only a parameterized constructor, no settable members
        _ = connection.Query<BinaryLike>("select_sql");
        // abstract types cannot be constructed at all
        _ = connection.Query<AbstractType>("select_sql");
        // control: an ordinary POCO still works
        _ = connection.Query<Poco>("select_sql");
    }

    public class PrivateCtor { private PrivateCtor() { } public int Value { get; set; } }
    public class BinaryLike { public BinaryLike(byte[] value) { } public int Length => 0; }
    public abstract class AbstractType { public int Value { get; set; } }
    public class Poco { public int Value { get; set; } }
}

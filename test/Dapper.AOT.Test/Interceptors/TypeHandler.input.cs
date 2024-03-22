using Dapper;
using System.Data.Common;

[module: DapperAot]
[module: TypeHandler<CustomClass, CustomClassTypeHandler>]

public class CustomClassTypeHandler : TypeHandler<CustomClass>
{
}

public class CustomClass
{
}

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<MyType>("def");
    }

    public class MyType
    {
        public CustomClass C { get; set; }
    }
}
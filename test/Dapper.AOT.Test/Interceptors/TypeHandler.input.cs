using Dapper;
using System.Data;
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
        _ = connection.Query<int>("def", new { Param = new CustomClass() });
        _ = connection.Query<int>("@OutputValue = def", new CommandParameters());
    }

    public class CommandParameters
    {
        [DbValue(Direction = ParameterDirection.Output)]
        public CustomClass OutputValue { get; set; }
    }

    public class MyType
    {
        public CustomClass C { get; set; }
    }
}
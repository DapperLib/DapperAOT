#nullable enable
using Dapper;
using System.Data;
using System.Data.Common;

// The fixture from external PRs #117 (samcragg) and #162 (7amou3) - which propose the same four
// scenarios, in the same shapes - translated to the registration and handler contract this PR
// ships. Call sites, type names and member names are kept verbatim so the two can be compared
// directly; only the two spellings differ:
//
//   theirs   [module: TypeHandler<CustomClass, CustomClassTypeHandler>]
//   here     [module: TypeHandler(typeof(CustomClass), typeof(CustomClassTypeHandler))]
//
//   theirs   class CustomClassTypeHandler : TypeHandler<CustomClass>
//   here     class CustomClassTypeHandler : DbValueHandler<CustomClass>
//
// The generic attribute cannot be read by .NET Framework's GetCustomAttributes (it throws for
// the whole call, poisoning unrelated reflection), hence the typeof form.

[module: DapperAot]
[module: TypeHandler(typeof(CustomClass), typeof(CustomClassTypeHandler))]

public class CustomClass
{
    public string? Value { get; set; }
}

public class CustomClassTypeHandler : DbValueHandler<CustomClass>
{
    // theirs left the handler empty, relying on base-class defaults; here SetValueCore and
    // Parse are abstract, because a handler that handles nothing is a mistake worth a compiler
    // error rather than a silent pass-through
    protected override void SetValueCore(DbParameter parameter, CustomClass value)
        => parameter.Value = value.Value;

    protected override CustomClass Parse(object? value)
        => new CustomClass { Value = value as string };
}

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        // (1) read: a member of the handled type, on a mapped row type
        _ = connection.Query<MyType>("def");

        // (2) write: a member of the handled type, on an *anonymous* parameter type
        _ = connection.Query<int>("def", new { Param = new CustomClass() });

        // (3) output parameter of the handled type, read back through the handler
        _ = connection.Query<int>("@OutputValue = def", new CommandParameters());
    }

    public class CommandParameters
    {
        [DbValue(Direction = ParameterDirection.Output)]
        public CustomClass? OutputValue { get; set; }
    }

    public class MyType
    {
        public CustomClass? C { get; set; }
    }
}

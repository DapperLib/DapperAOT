using Dapper;
using System.Data.Common;

[module: DapperAot]

// CommandDefinition overloads are not supported, and - unlike the other unsupported APIs -
// nothing tells the consumer so: the analyzer only inspects call-sites with a string `sql`
// parameter, and these carry the SQL inside the struct. They therefore fall back to vanilla
// Dapper silently, which works under JIT and fails under native AOT with no build-time signal.
// Pinned here so the count is visible: the scorecard reports them as *skipped silently*.
public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // for contrast: the plain overload generates
        _ = connection.Query<int>("select Id from Users where Id = @id", new { id = 1 });

        _ = connection.Query<int>(new CommandDefinition("select Id from Users where Id = @id", new { id = 1 }));
        _ = connection.Execute(new CommandDefinition("delete Users where Id = @id", new { id = 1 }));
    }
}

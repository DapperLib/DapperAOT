using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var obj = new { A = 1, B = 2, C = 3 };
        var arr = new[] { obj };

        // fine, text, detect b
        connection.Execute("foo @b", obj);

        // error, d not mapped
        connection.Execute("foo @d", obj);

        // error, no args supplied
        connection.Execute("foo @b");

        // warning, args not needed
        connection.Execute("foo bar", obj);

        // and the same, but with batches

        // fine, text, detect b
        connection.Execute("foo @b", arr);

        // error, d not mapped
        connection.Execute("foo @d", arr);

        // warning, args not needed
        connection.Execute("foo bar", arr);
    }
}
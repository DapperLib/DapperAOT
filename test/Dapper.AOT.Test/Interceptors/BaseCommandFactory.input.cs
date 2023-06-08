using Dapper;
using System;
using System.Data;
using System.Data.Common;

[module: CommandFactory<SomethingAwkward.MyCommandFactory<object>>]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.Execute("def", obj);
    }
}

namespace SomethingAwkward
{
    abstract class MyCommandFactory<T> : CommandFactory<T>
    {
        public override DbCommand Prepare(DbConnection connection, string sql, CommandType commandType, T args)
        {
            Console.WriteLine(nameof(Prepare));
            return base.Prepare(connection, sql, commandType, args);
        }

        public override bool PostProcess(DbCommand command, T args)
        {
            Console.WriteLine(nameof(PostProcess));
            return base.PostProcess(command, args);
        }
    }
}
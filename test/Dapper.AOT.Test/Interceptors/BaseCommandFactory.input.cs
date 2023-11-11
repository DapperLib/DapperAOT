using Dapper;
using System;
using System.Data;
using System.Data.Common;

[module: DapperAot]
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
        public override DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, T args)
        {
            Console.WriteLine(nameof(GetCommand));
            return base.GetCommand(connection, sql, commandType, args);
        }

        public override void PostProcess(in UnifiedCommand command, T args, int rowcount)
        {
            Console.WriteLine(nameof(PostProcess));
            base.PostProcess(command, args, rowcount);
        }
    }
}
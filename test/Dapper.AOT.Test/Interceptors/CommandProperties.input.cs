using Dapper;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Data.SqlClient;
[module: DapperAot]

public static class Foo
{
    [CacheCommand(true)]
    [CommandProperty<OracleCommand>(nameof(OracleCommand.FetchSize), 1024)]
    [CommandProperty<SqlCommand>("InvalidName", true)] // validate no SqlCommand test emitted since invalid
    static void Cached(DbConnection connection, string bar)
    {
        _ = connection.Query<Customer>("abc", new { Foo = 12 });

        _ = connection.Query<Customer>("def", new { Foo = 12, bar });
        _ = connection.Query<Customer>("def", new { Foo = 12, bar });
    }

    [CommandProperty<OracleCommand>(nameof(OracleCommand.FetchSize), 1024)]
    static void NonCached(DbConnection connection, string bar)
    {
        _ = connection.Query<Customer>("abc", new { Foo = 12 });

        _ = connection.Query<Customer>("def", new { Foo = 12, bar });
        _ = connection.Query<Customer>("def", new { Foo = 12, bar });
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
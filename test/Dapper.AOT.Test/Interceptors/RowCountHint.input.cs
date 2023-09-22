using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    [RowCountHint(25)]
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<Customer>("def");
        _ = connection.Query<Customer>("def", new { Foo = 12, bar });
        _ = connection.Query<Customer>("def", new DynamicHint { Foo = 12, Bar = bar, Count = 22 });
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
    public class DynamicHint
    {
        public int Foo { get; set; }
        public string Bar { get; set; }
        [RowCountHint]
        public int Count { get; set; }
    }
}
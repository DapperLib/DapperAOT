using Dapper;
using System.Data;
using System.Data.Common;

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.QueryFirst<Customer>("def", obj);
        _ = connection.QueryFirstOrDefault<Customer>("def", obj, commandType: CommandType.StoredProcedure);
        _ = connection.QuerySingle<Customer>("def", obj, commandType: CommandType.Text);
        _ = connection.QuerySingleOrDefault<Customer>("def");
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
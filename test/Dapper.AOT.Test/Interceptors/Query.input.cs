using Dapper;
using System.Data;
using System.Data.Common;

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.Query<Customer>("def", obj);
        _ = connection.Query<Customer>("def", obj, buffered: isBuffered);
        _ = connection.Query<Customer>("def", buffered: false, commandType: CommandType.StoredProcedure);
        _ = connection.Query<Customer>("def", obj, buffered: true, commandType: CommandType.Text);
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
using Dapper;
using System.Data;
using System.Data.Common;

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.ExecuteScalar("def", obj);
        _ = connection.ExecuteScalar("def", commandType: CommandType.StoredProcedure);
        _ = connection.ExecuteScalar("def", commandType: CommandType.Text);

        _ = connection.ExecuteScalar<float>("def", obj);
        _ = connection.ExecuteScalar<float>("def", commandType: CommandType.StoredProcedure);
        _ = connection.ExecuteScalar<float>("def", commandType: CommandType.Text);
    }
    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
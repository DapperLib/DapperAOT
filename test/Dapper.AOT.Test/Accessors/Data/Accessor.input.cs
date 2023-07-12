using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar)
    {
        var customer = new Customer { X = 1, Y = "hello", Z = 0.1 };
        _ = TypeAccessor.CreateAccessor(customer);
    }

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
        public State State { get; set; }
    }
    public enum State
    {
        Active,
        Disabled
    }
}
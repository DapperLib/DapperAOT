using System;
using Dapper;

[module: DapperAot]

public static class Foo
{
    static void SomeCode()
    {
        var customer = new Customer { X = 1, Y = "hello", Z = 0.1 };
        _ = TypeAccessor.CreateAccessor(customer);

        var customers = new[] { customer };
        _ = TypeAccessor.CreateDataReader(customers);
    }

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
        public State State { get; set; }
        public object Obj { get; set; }
        public DBNull DbNullProp { get; set; }
    }
    public enum State
    {
        Active,
        Disabled
    }
}
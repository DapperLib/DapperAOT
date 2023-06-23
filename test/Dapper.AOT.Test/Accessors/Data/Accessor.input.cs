using Dapper;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar)
    {
        var customers = new List<Customer>();
        _ = TypeAccessor.CreateReader(customers);   
    }

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
using Dapper;
using System.Collections.Generic;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // enumerable parameters mean multi-exec, which is only valid for Execute;
        // vanilla Dapper throws for all of these, so they must not be intercepted
        // (previously the array-of-anonymous case emitted unparseable code)
        _ = connection.Query<Customer>("select X from Customers where X in @ids", new[] { new Customer { X = 1 } });
        _ = connection.Query("select X from Customers", new List<Customer>());
        _ = connection.Query("select X from Customers", new[] { new { Id = 1 } });
        _ = connection.ExecuteScalar("select count(1) from Customers", new Customer[0]);

        // multi-exec itself is still fine
        connection.Execute("insert Customers (X) values (@X)", new List<Customer> { new Customer { X = 2 } });
        connection.Execute("insert Customers (Id) values (@Id)", new[] { new { Id = 1 } });
    }
    public class Customer { public int X {get;set;} }
}

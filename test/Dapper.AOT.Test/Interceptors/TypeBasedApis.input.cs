using Dapper;
using System.Data.Common;

[module: DapperAot]

// Type-argument overloads choose the row type at execution time, which is the one thing
// compile-time generation cannot follow. Declared a non-goal (2026-08-26) rather than building a
// Type-keyed registry: DAP056 says so at the call-site and names the generic form to use.
public static class Foo
{
    static void SomeCode(DbConnection connection, DbDataReader reader)
    {
        // the generic forms: supported, and what DAP056 points people at
        _ = connection.Query<Customer>("select * from Customers");
        _ = reader.GetRowParser<Customer>();

        // ...and the Type-based forms: all refused, all reported
        _ = connection.Query(typeof(Customer), "select * from Customers");
        _ = connection.QueryFirst(typeof(Customer), "select top 1 * from Customers");
        _ = connection.QuerySingleOrDefault(typeof(Customer), "select * from Customers where Id = 1");
        _ = reader.GetRowParser(typeof(Customer));

        // an explicitly-passed concreteType is Type-based; omitting it (above) is not
        _ = reader.GetRowParser<Customer>(concreteType: typeof(Customer));
    }
}
public class Customer { public int Id { get; set; } }

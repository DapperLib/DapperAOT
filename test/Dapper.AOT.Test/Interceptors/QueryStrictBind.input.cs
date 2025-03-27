using Dapper;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]
public static class Foo
{
    [QueryColumns("X", null, "Z", "", "Y")]
    static Task<Customer> SomeCode1(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    [QueryColumns("X", null, "Z", "", "Y")] // should be shared with 1
    static Task<Customer> SomeCode2(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    [QueryColumns("X", null, "Z", "", "Y"), StrictTypes]
    static Task<Customer> SomeCode3(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    [QueryColumns("X", null, "Z", "", "Y"), StrictTypes] // should be shared with 3
    static Task<Customer> SomeCode4(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    [StrictTypes]
    static Task<Customer> SomeCode5(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    [StrictTypes] // should be shared with 5
    static Task<Customer> SomeCode6(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    static Task<Customer> SomeCode7(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    // should be shared with 7
    static Task<Customer> SomeCode8(DbConnection connection, string bar, bool isBuffered)
        => connection.QuerySingleAsync<Customer>("select 1,2,3,4,5");

    public class Customer
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }
}
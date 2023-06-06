#pragma warning disable CA1050, CA1822
using Dapper;
using System;
using System.Data.Common;

public static class Foo
{
    [DapperAot(true)]
    public static Customer GetCustomer(DbConnection connection, int id, string region)
    {
        return connection.QueryFirst<Customer>("select * from Customers where id=@id and region=@region",
            new { id, region});
    }
    public class Customer
    {
        public int X { get; set; }
        public string? Y;
        public Guid? Z { get; set; }
    }
}



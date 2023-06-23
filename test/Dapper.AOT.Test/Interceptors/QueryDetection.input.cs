using Dapper;
using System.Data.SqlClient;

[module: DapperAot]
public static class Foo
{
    static void SystemData(SqlConnection db, Customer customer)
    {
        // fine
        db.Execute("insert Customers (Name) values (@name)", customer);
        // should warn: has query
        db.Execute("insert Customers (Name) output INSERTED.Id values (@name)", customer);

        // fine
        db.QuerySingle<int>("insert Customers (Name) output INSERTED.Id values (@name)", customer);
        db.ExecuteScalar<int>("insert Customers (Name) output INSERTED.Id values (@name)", customer);
        db.QuerySingle<Customer>("select * from Customers");
        db.ExecuteScalar<int>("select 42");

        // should error: no query
        db.QuerySingle<int>("insert Customers (Name) values (@name)", customer);
        db.ExecuteScalar<int>("insert Customers (Name) values (@name)", customer);

        // fine, no way of knowing
        db.Execute("EXEC someproc");
        db.QuerySingle<int>("EXEC someproc");
        db.ExecuteScalar<int>("EXEC someproc");
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
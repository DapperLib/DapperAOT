using Dapper;

[module: DapperAot]
public static class Foo
{
    const string InsertSql = """
        declare @tmp int = 1;
        insert Customers (Id, Name, X, Y)
        values (@Id, @Name, @tmp, @Missing)

        return 1;
        """;
    // expect these to detect invalid SQL and that 'Missing' is not bound
    static void SystemData(System.Data.SqlClient.SqlConnection db, Customer customer)
    {
        db.Execute("insert customers (id) asdasdlk kals");

        db.Execute(InsertSql, customer);
    }
    static void MicrosoftData(Microsoft.Data.SqlClient.SqlConnection db, Customer customer)
    {
        db.Execute("insert customers (id) asdasdlk kals");

        db.Execute(InsertSql, customer);
    }
    // don't expect this to be found
    static void MicrosoftData(System.Data.Common.DbConnection db, Customer customer)
    {
        db.Execute("insert customers (id) asdasdlk kals");

        db.Execute(InsertSql, customer);
    }

    public class Customer
    {
        public int Id { get; set; }
        public int Name { get; set; }
    }
}
using Dapper;

[module: DapperAot]
public class Foo
{
    public void Things(System.Data.SqlClient.SqlConnection connection)
    {
        var args = new { a = 123, b = "abc" };
        int[] ids = { 1, 2, 3 };

        // should warn: specify INSERT columns
        connection.Execute("insert SomeTable values (@a, @b)", args);
        // great!
        connection.Execute("insert SomeTable (A, B) values (@a, @b)", args);
        // column count mismatch
        connection.Execute("insert SomeTable (A, B, C) values (@a, @b)", args);
        // unbalanced multi-row
        connection.Execute("insert SomeTable (A, B) values (@a, @b), (@a, @b, 123)", args);
        // multi-row (fine)
        connection.Execute("insert SomeTable (A, B) values (@a, @b), (@a, @b)", args);

        // should warn: specify SELECT columns
        _ = connection.QuerySingle("select * from SomeTable");

        // should warn: specify SELECT columns
        _ = connection.QuerySingle("select a.* from SomeTable a");

        // great!
        _ = connection.QuerySingle("select A, B from SomeTable");

        // custom dapper "in" syntax
        _ = connection.QuerySingle("select A from SomeTable where id in @ids", new { ids });

        // custom dapper "literal" syntax
        _ = connection.QuerySingle("select A from SomeTable where id={=a}", args);

        // missing column name
        _ = connection.Query("select Credit - Debit from Accounts");

        // fine
        _ = connection.Query("select Balance = Credit - Debit from Accounts");
        _ = connection.Query("select (Credit - Debit) as Balance from Accounts");

        // duplicate column name
        _ = connection.Query("select Balance, (Credit - Debit) as Balance from Accounts");

        // this is valid, but inefficient
        _ = connection.Query("""
            declare @id int = 0;
            select @id = @id + Balance from Accounts
            select @id as Sum
            """);

        // this is invalid SQL (assign plus read)
        _ = connection.Query("""
            declare @id int = 0;
            select @id = @id + Balance, Credit from Accounts
            select @id as [Id]
            """);
    }
}
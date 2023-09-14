using Dapper;

[module: DapperAot]
public class Foo
{
    public void Things(System.Data.SqlClient.SqlConnection connection)
    {
        var args = new { a = 123, b = "abc" };

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
    }
}
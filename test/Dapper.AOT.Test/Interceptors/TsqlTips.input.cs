using Dapper;

[module: DapperAot]
public class Foo
{
    public void TopChecks(System.Data.SqlClient.SqlConnection connection)
    {
        // fine
        connection.Query("select A, B from SomeTable");
        connection.Query("select TOP 10 A, B from SomeTable");
        // non-positive top
        connection.Query("select TOP 0 A, B from SomeTable");
        // non integer top
        connection.Query("select TOP 5.0 A, B from SomeTable");
        // non integer percent top (fine)
        connection.Query("select TOP 5.0 PERCENT A, B from SomeTable");
        connection.Query("select TOP 5.1 PERCENT A, B from SomeTable");
        // non integer negative percent top (not fine)
        connection.Query("select TOP -5.1 PERCENT A, B from SomeTable");

        // first/single with where: fine
        connection.QueryFirst("select A, B from SomeTable where Id=42");
        connection.QuerySingle("select A, B from SomeTable where Id=42");
        // first/single with top and order-by: fine
        connection.QueryFirst("select top 1 A, B from SomeTable order by B");
        connection.QuerySingle("select top 2 A, B from SomeTable order by B");
        // first/single with wrong top
        connection.QueryFirst("select top 2 A, B from SomeTable order by B");
        connection.QuerySingle("select top 1 A, B from SomeTable order by B");
        // first/single with nothing
        connection.QueryFirst("select A, B from SomeTable");
        connection.QuerySingle("select A, B from SomeTable");
        // first/single with order-by but no top, or top but no order-by
        connection.QueryFirst("select A, B from SomeTable order by B");
        connection.QuerySingle("select A, B from SomeTable order by B");
        connection.QueryFirst("select top 1 A, B from SomeTable");
        connection.QuerySingle("select top 2 A, B from SomeTable");

        // first/single with join; we'll allow join to function as a filter
        connection.QueryFirst("select x.A, B from SomeTable x inner join Foo y on x.Id = y.Id");

        // check for aliases
        connection.QueryFirst("select A, B from SomeTable x inner join Foo on x.Id = FromSomewhere");

    }

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

        // typed: missing name
        _ = connection.QueryFirst<Customer>("select (Credit - Debit) from Accounts");
        // untyped: fine, bound by index
        _ = connection.QueryFirst<int>("select (Credit - Debit) from Accounts");
    }
    [BindTupleByName(true)]
    public void TupleByName(System.Data.SqlClient.SqlConnection connection)
    {
        // untyped, but bound by name; dodgy
        _ = connection.QueryFirst<(int a, int b)>("select (Credit - Debit) from Accounts");
    }
    [BindTupleByName(false)]
    public void TupleByIndex(System.Data.SqlClient.SqlConnection connection)
    {
        // untyped: fine, bound by index
        _ = connection.QueryFirst<(int, int)>("select (Credit - Debit) from Accounts");
    }

    public void ExecuteRules(System.Data.SqlClient.SqlConnection connection)
    {
        // execute with result
        connection.Execute("""
            declare @id int = 0;
            select @id = @id + Balance from Accounts

            select @id = @id + 1;

            select @id as Sum
            """);

        // execute *without* a result, despite the select
        connection.Execute("""
            declare @id int = 0;
            select @id = @id + Balance
            from Accounts

            select @id = @id + 1;

            update SomeTable
            set Whatever = @id
            where [Key]=1
            """);

        // delete/update with filter: fine
        connection.Execute("delete from Customers where Id=12");
        connection.Execute("update Customers set Name='New name' where Id=12");
        // allow join to function like filter
        connection.Execute("delete c from Customers c inner join Foo y on y.Id = c.Id");

        // delete/update without filter: warn
        connection.Execute("delete from Customers");
        connection.Execute("update Customers set Name='New name'");
        // allow join to function like filter
        connection.Execute("update c set c.Name='New name' from Customers c inner join Foo y on y.Id = c.Id");

        connection.QuerySingle<Customer>("select Id from Customers where Id=42");
        connection.QuerySingle<Customer>("select top 1 Id from Customers");
        connection.QuerySingle<Customer>("select top 2 Id from Customers");
        // > 2 is bad for Single etc (2 makes sense to check for dups)
        connection.QuerySingle<Customer>("select top 3 Id from Customers");
        // no where
        connection.QuerySingle<Customer>("select Id from Customers");
    }

    public void TopChecks(System.Data.SqlClient.SqlConnection typed, System.Data.Common.DbConnection untyped)
    {   // for issue #44 https://github.com/DapperLib/DapperAOT/issues/44
        typed.Execute("""
            DECLARE @ActiveWidgets TABLE  (Id int)
            INSERT @ActiveWidgets (Id) SELECT Id FROM Widgets WHERE Active = 1
            """);

        untyped.Execute("""
            DECLARE @ActiveWidgets TABLE  (Id int)
            INSERT @ActiveWidgets (Id) SELECT Id FROM Widgets WHERE Active = 1
            """);
    }

    public class Customer
    {
        public int Balance;
    }
}
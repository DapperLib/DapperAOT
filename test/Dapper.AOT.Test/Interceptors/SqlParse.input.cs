﻿using Dapper;

[module: DapperAot]
public static class Foo
{
    const string BadSql = "insert customers (id) "
        + " as asdasdlk kals";

    const string InsertSql = """
        declare @tmp int = 1;
        insert Customers (Id, Name, X, Y)
        values (@Id, @Name, @tmp, @Missing)

        return 1;
        """;

    const string AllTheFail = """
set @id = 3; -- 211
declare @id int = 1;

declare @id int = 1; -- 202
GO -- 201

select SCOPE_IDENTITY(), @@IDENTITY -- 203, 204
select * from Customers where Name = null and null < Id -- 205

declare @t table (id int not null)

select * from @t -- 209
insert @t (id) values (4)
select * from @t -- should be fine

set @t = 4 -- 208

declare @s int
select @s -- 210
set @s = 2
select @s -- should be fine

insert @id (id) values (4) -- 207

gibb!eri -- 206

""";
    // expect these to detect invalid SQL and that 'Missing' is not bound
    static void SystemData(System.Data.SqlClient.SqlConnection db, Customer customer)
    {
        db.Execute(BadSql);

        db.Execute(InsertSql, customer);

        db.Execute(AllTheFail, customer);
    }
    static void MicrosoftData(Microsoft.Data.SqlClient.SqlConnection db, Customer customer)
    {
        db.Execute(BadSql);

        db.Execute(InsertSql, customer);

        db.Execute(AllTheFail, customer);
    }
    // don't expect this to be found
    static void GeneralDatabase(System.Data.Common.DbConnection db, Customer customer)
    {
        db.Execute(BadSql);

        db.Execute(InsertSql, customer);

        db.Execute(AllTheFail, customer);
    }

    public class Customer
    {
        public int Id { get; set; }
        public int Name { get; set; }
    }
}
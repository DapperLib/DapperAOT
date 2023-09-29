using Dapper;
using System.Data.SqlClient;

[module: DapperAot]
public static class Foo
{
    // the point here is: line/column numbers of literals
    static void SystemData(SqlConnection db)
    {
        const string abc = nameof(abc);

        _ = db.Query("select 'abc' as A;select @@identity as B;select 'def' as C;");
        _ = db.Query("select 'abc' as A;\r\nselect @@identity as B;select 'def' as C;");
        _ = db.Query($"select '{abc}' as A;select @@identity as B;select 'def' as C;");
        _ = db.Query("select \"abc\" as A;select @@identity as B;select \"abc\" as C;");
        _ = db.Query(@"select 'abc' as A;select @@identity as B;select 'def' as C;");
        _ = db.Query($@"select '{abc}' as A;select @@identity as B;select 'def' as C;");
        _ = db.Query(@"select ""abc"" as A;select @@identity as B;select 'def' as C;");
        _ = db.Query(@"select 'abc' as A;
select @@identity as B;
select 'def' as C;");
        _ = db.Query("""select 'abc' as A;select @@identity as B;select 'def' as C;""");
        _ = db.Query("""
            select 'abc' as A;
            select @@identity as B;
            select 'def' as C;
            """);
        _ = db.Query($"""
            select '{abc}' as A;
            select @@identity as B;
            select 'def' as C;
            """);
        _ = db.Query("select 'abc' as A;" + ";select @@identity as B;" + "select 'def' as C;");

        const string S1 = "select 'abc' as A;select @@identity as B;select 'def' as C;";
        _ = db.Query(S1);
        const string S2 = $"select '{abc}' as A;select @@identity as B;select 'def' as C;";
        _ = db.Query(S2);
        const string S3 = "select \"abc\" as A;select @@identity as B;select \"abc\" as C;";
        _ = db.Query(S3);
        const string S4 = @"select 'abc' as A;select @@identity as B;select 'def' as C;";
        _ = db.Query(S4);
        const string S5 = $@"select '{abc}' as A;select @@identity as B;select 'def' as C;";
        _ = db.Query(S5);
        const string S6 = @"select ""abc"" as A;select @@identity as B;select 'def' as C;";
        _ = db.Query(S6);
        const string S7 = @"select 'abc' as A;
select @@identity as B;
select 'def' as C;";
        _ = db.Query(S7);
        const string S8 = """select 'abc' as A;select @@identity as B;select 'def' as C;""";
        _ = db.Query(S8);
        const string S9 = """
            select 'abc' as A;
            select @@identity as B;
            select 'def' as C;
            """;
        _ = db.Query(S9);
        const string S10 = $"""
            select '{abc}' as A;
            select @@identity as B;
            select 'def' as C;
            """;
        _ = db.Query(S10);
        const string S11 = "select 'abc' as A;" + ";select @@identity as B;" + "select 'def' as C;";
        _ = db.Query(S11);


    }
}
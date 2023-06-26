using Dapper;
using System.Data.SqlClient;

[module: DapperAot]
public static class Foo
{
    // the point here is: line/column numbers of literals
    static void SystemData(SqlConnection db)
    {
        const string abc = nameof(abc);

        _ = db.Query("select 'abc';select @@identity;select 'def';");
        _ = db.Query("select 'abc';\r\nselect @@identity;select 'def';");
        _ = db.Query($"select '{abc}';select @@identity;select 'def';");
        _ = db.Query("select \"abc\";select @@identity;select \"abc\";");
        _ = db.Query(@"select 'abc';select @@identity;select 'def';");
        _ = db.Query($@"select '{abc}';select @@identity;select 'def';");
        _ = db.Query(@"select ""abc"";select @@identity;select 'def';");
        _ = db.Query(@"select 'abc';
select @@identity;
select 'def';");
        _ = db.Query("""select 'abc';select @@identity;select 'def';""");
        _ = db.Query("""
            select 'abc';
            select @@identity;
            select 'def';
            """);
        _ = db.Query($"""
            select '{abc}';
            select @@identity;
            select 'def';
            """);
        _ = db.Query("select 'abc';" + ";select @@identity;" + "select 'def';");

        const string S1 = "select 'abc';select @@identity;select 'def';";
        _ = db.Query(S1);
        const string S2 = $"select '{abc}';select @@identity;select 'def';";
        _ = db.Query(S2);
        const string S3 = "select \"abc\";select @@identity;select \"abc\";";
        _ = db.Query(S3);
        const string S4 = @"select 'abc';select @@identity;select 'def';";
        _ = db.Query(S4);
        const string S5 = $@"select '{abc}';select @@identity;select 'def';";
        _ = db.Query(S5);
        const string S6 = @"select ""abc"";select @@identity;select 'def';";
        _ = db.Query(S6);
        const string S7 = @"select 'abc';
select @@identity;
select 'def';";
        _ = db.Query(S7);
        const string S8 = """select 'abc';select @@identity;select 'def';""";
        _ = db.Query(S8);
        const string S9 = """
            select 'abc';
            select @@identity;
            select 'def';
            """;
        _ = db.Query(S9);
        const string S10 = $"""
            select '{abc}';
            select @@identity;
            select 'def';
            """;
        _ = db.Query(S10);
        const string S11 = "select 'abc';" + ";select @@identity;" + "select 'def';";
        _ = db.Query(S11);


    }
}
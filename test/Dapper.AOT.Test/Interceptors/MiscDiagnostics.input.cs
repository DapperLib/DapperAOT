using Dapper;
using System.Data.Common;



public static class SomeCode
{
    [DapperAot(true)]
    public static void WithDapper(DbConnection connection, object args)
    {
        // expect non-constant SQL
        connection.Execute(System.Console.ReadLine());

        // expect warning that BindByName should be specified
        // expect tuple-type results not supported
        connection.QueryFirst<(int Id, string Name)>("blah");

        // expect warning that BindByName should be specified
        // expect tuple-type params not supported
        connection.Execute("blah", (Id: 42, Name: "abc"));

        // expect warning that args type unknown
        connection.Execute("blah", args);

        // no warning expected, this is fine
        connection.Execute("blah");
    }

    [DapperAot(true), BindByName]
    public static void WithDapperAndBindByName(DbConnection connection)
    {
        // expect tuple-type results not supported
        connection.QueryFirst<(int Id, string Name)>("blah");

        // expect tuple-type params not supported
        connection.Execute("blah", (Id: 42, Name: "abc"));
    }

    [DapperAot(false), BindByName]
    public static void WithoutDapper(DbConnection connection)
    {
        // do not expect non-constant SQL warning; legacy Dapper is fine with this
        connection.Execute(System.Console.ReadLine());

        // expect warning that dapper doesn't support bind-by-name tuple results
        connection.QueryFirst<(int Id, string Name)>("blah");
    }
}
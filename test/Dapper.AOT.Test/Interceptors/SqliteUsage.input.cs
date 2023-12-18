using Dapper;
using System.Collections.Generic;
using System.Data.Common;

namespace Dapper.AOT.Test.Integration;

[DapperAot]
static class SqliteUsage
{
    public static void Insert(DbConnection connection, string name, SomeEnum value)
        => connection.Execute("INSERT INTO Foo(Name, Value) VALUES ($name, $value)", new { name, value });

    public static List<TypeWithSomeEnum> GetAll(DbConnection connection)
        => connection.Query<TypeWithSomeEnum>("SELECT Id, Name, Value FROM Foo").AsList();
}

public class TypeWithSomeEnum
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SomeEnum Value { get; set; }
}

public enum SomeEnum
{
    A = 1, B = 2, C = 3,
}
using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // a dynamic-typed member: the tokenizer's type test must use object, since
        // typeof(dynamic) is not legal C# (and the reader reports object anyway)
        _ = connection.Query<HazDynamic>("select_sql");
    }

    public class HazDynamic
    {
        public dynamic Id { get; set; }
        public string Name { get; set; }
    }
}

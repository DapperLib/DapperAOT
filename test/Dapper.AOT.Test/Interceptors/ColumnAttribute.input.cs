using Dapper;
using System.Data.Common;

// needed for [Column] attribute
using System.ComponentModel.DataAnnotations.Schema;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<MyType>("def");
    }

    public class MyType
    {
        // default
        public int A { get; set; }

        // dbvalue
        [DbValue(Name = "DbValue_B")]
        public int B { get; set; }

        // standard enabled
        [UseColumnAttribute]
        [Column("StandardColumn_C")]
        public int C { get; set; }

        // explicitly enabled
        [UseColumnAttribute]
        [Column("ExplicitColumn_D")]
        public int D { get; set; }

        // dbValue & Column enabled
        [UseColumnAttribute]
        [DbValue(Name = "DbValue_E")]
        [Column("Column_E")]
        public int E { get; set; }
    }
}
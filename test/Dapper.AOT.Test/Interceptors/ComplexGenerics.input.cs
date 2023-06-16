using Dapper;
using System.Data.Common;

[module: DapperAot]
public static class SomeLayer
{
    public class Foo
    {
        public class Bar<T>
        {
            public class Blap
            {
                public int Id { get; set; }
                public static void SomeCode(DbConnection connection, string bar)
                {
                    _ = connection.Execute("def", new Blap {  Id = 42 });
                    _ = connection.Query<Blap>("def");
                }
            }
        }
    }
}
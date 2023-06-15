//using Dapper;
//using System.Collections.Immutable;
//using System.Data;
//using System.Data.Common;
//
//[module: DapperAot]
//
//public static class Foo
//{
//    static void SomeCode(DbConnection connection, string bar)
//    {
//        _ = connection.Execute("def", (new[] { new { Foo = 12, bar }, new { Foo = 53, bar = "abc" } }).AsList());
//        _ = connection.Execute("def", new Customer[0], commandType: CommandType.StoredProcedure);
//        _ = connection.Execute("def", ImmutableArray<Customer>.Empty, commandType: CommandType.Text);
//    }
//    public class Customer
//    {
//        public int X { get; set; }
//        public string Y;
//        public double? Z { get; set; }
//    }
//}
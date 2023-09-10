using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<PublicPropertiesNoConstructor>("def");
        _ = connection.Query<MultipleFactoryMethods>("def");
    }

    public class PublicPropertiesNoConstructor
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }

        [DapperAot(true)]
        public static PublicPropertiesNoConstructor Construct(int x, string y, double? z) 
            => new PublicPropertiesNoConstructor { X = x, Y = y, Z = z };
    }

    public class MultipleFactoryMethods
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }

        [DapperAot(true)]
        public static MultipleFactoryMethods Construct(int x, string y, double? z) 
            => new MultipleFactoryMethods { X = x, Y = y, Z = z };
        
        [DapperAot(true)]
        public static MultipleFactoryMethods Construct2(int x, string y, double? z) 
            => new MultipleFactoryMethods { X = x, Y = y, Z = z };
    }
}
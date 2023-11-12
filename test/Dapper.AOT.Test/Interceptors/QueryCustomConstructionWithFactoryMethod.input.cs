using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<PublicPropertiesNoConstructor>("def");
        _ = connection.Query<MultipleDapperAotFactoryMethods>("def");
        _ = connection.Query<SingleFactoryNotMarkedWithDapperAot>("def");
        _ = connection.Query<MultipleStandardFactoryMethods>("def");
    }

    public class PublicPropertiesNoConstructor
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }

        [ExplicitConstructor]
        public static PublicPropertiesNoConstructor Construct(int x, string y, double? z) 
            => new PublicPropertiesNoConstructor { X = x, Y = y, Z = z };
    }

    public class MultipleDapperAotFactoryMethods
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }

        [ExplicitConstructor]
        public static MultipleDapperAotFactoryMethods Construct(int x, string y, double? z) 
            => new MultipleDapperAotFactoryMethods { X = x, Y = y, Z = z };
        
        [ExplicitConstructor]
        public static MultipleDapperAotFactoryMethods Construct2(int x, string y, double? z) 
            => new MultipleDapperAotFactoryMethods { X = x, Y = y, Z = z };
    }
    
    public class SingleFactoryNotMarkedWithDapperAot
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }
        
        public static SingleFactoryNotMarkedWithDapperAot Construct(int x, string y, double? z) 
            => new SingleFactoryNotMarkedWithDapperAot { X = x, Y = y, Z = z };
    }
    
    public class MultipleStandardFactoryMethods
    {
        public int X { get; set; }
        public string Y { get; set; }
        public double? Z { get; set; }
        
        public static MultipleStandardFactoryMethods Construct1(int x, string y, double? z) 
            => new MultipleStandardFactoryMethods { X = x, Y = y, Z = z };
        
        public static MultipleStandardFactoryMethods Construct2(int x, string y, double? z) 
            => new MultipleStandardFactoryMethods { X = x, Y = y, Z = z };
    }
}
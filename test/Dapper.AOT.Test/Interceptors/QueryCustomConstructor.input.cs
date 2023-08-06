using Dapper;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<ParameterlessCtor>("def");
        _ = connection.Query<OnlyNonDapperAotCtor>("def");
        _ = connection.Query<SingleDefaultCtor>("def");
        _ = connection.Query<MultipleDapperAotCtors>("def");
        _ = connection.Query<SingleDapperAotCtor>("def");
    }

    public class ParameterlessCtor
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }

        public ParameterlessCtor()
        {

        }
    }

    public class OnlyNonDapperAotCtor
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }

        public OnlyNonDapperAotCtor() { }

        [DapperAot(false)]
        public OnlyNonDapperAotCtor(int x) { }

        [DapperAot(false)]
        public OnlyNonDapperAotCtor(int x, int y) { }
    }

    public class SingleDefaultCtor
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }

        [DapperAot(false)]
        public SingleDefaultCtor() { }

        [DapperAot(false)]
        public SingleDefaultCtor(int x) { }

        public SingleDefaultCtor(int x, string y, double? z) 
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class MultipleDapperAotCtors
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }

        public MultipleDapperAotCtors() { }

        [DapperAot(true)]
        public MultipleDapperAotCtors(int x)
        {
            X = x;
        }

        [DapperAot(true)]
        public MultipleDapperAotCtors(int x, string y, double? z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class SingleDapperAotCtor
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }

        public SingleDapperAotCtor() { }

        public SingleDapperAotCtor(int x) { }

        [DapperAot(true)]
        public SingleDapperAotCtor(int x, string y, double? z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
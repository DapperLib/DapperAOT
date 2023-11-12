using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection, string bar, bool isBuffered)
    {
        _ = connection.Query<ParameterlessCtor>("def");
        _ = connection.Query<GetOnlyPropertiesViaConstructor>("def");
        _ = connection.Query<RecordClass>("def");
        _ = connection.Query<RecordClassSimpleCtor>("def");
        _ = connection.Query<RecordStruct>("def");
        _ = connection.Query<RecordStructSimpleCtor>("def");
        _ = connection.Query<InitPropsOnly>("def");
        _ = connection.Query<InitPropsAndDapperAotCtor>("def");
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

    public class GetOnlyPropertiesViaConstructor
    {
        public int X { get; }
        public string Y { get; }
        public double? Z { get; }

        public GetOnlyPropertiesViaConstructor(int x, string y, double? z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public record RecordClass
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }

    public record RecordClassSimpleCtor(int X, string Y, double? Z);

    public record struct RecordStruct
    {
        public int X { get; set; }
        public string Y;
        public double? Z { get; set; }
    }

    public record struct RecordStructSimpleCtor(int X, string Y, double? Z);

    public class InitPropsOnly
    {
        public int X { get; init; }
        public string Y { get; init; }
        public double? Z { get; init; }
    }

    public class InitPropsAndDapperAotCtor
    {
        public int X { get; init; }
        public string Y;
        public double? Z { get; init; }

        [DapperAot(true)]
        public InitPropsAndDapperAotCtor(string y)
        {
            Y = y;
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

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
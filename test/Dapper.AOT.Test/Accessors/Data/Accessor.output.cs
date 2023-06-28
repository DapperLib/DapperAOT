#nullable enable
file static class DapperTypeAccessorGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Accessors\\Data\\Accessor.input.cs", 13, 26)]
    public static ObjectAccessor<T> CreateReader<T>(T obj, [DapperAot] TypeAccessor<T>? accessor = null)
    {
        return DapperCustomTypeAccessor0.Instance;
    }

    private sealed class DapperCustomTypeAccessor0 : global::Dapper.TypeAccessor<Foo.Customer>
    {
        internal static readonly DapperCustomTypeAccessor0 Instance = new();
        public override int MemberCount => 3;
        public override int? TryIndex(string name, bool exact = false) => name switch
        {
            nameof(Foo.Customer.X) => 0,
            nameof(Foo.Customer.Y) => 1,
            nameof(Foo.Customer.Z) => 2,
            _ => base.TryIndex(name, exact)
        };
        public override string GetName(int index) => index switch
        {
            0 => nameof(Foo.Customer.X),
            1 => nameof(Foo.Customer.Y),
            2 => nameof(Foo.Customer.Z),
            _ => base.GetName(index)
        };
        public override object? this[Foo.Customer obj, int index]
        {
            get => index switch
            {
                0 => obj.X,
                1 => obj.Y,
                2 => obj.Z,
                _ => base[obj, index]
            };
            set
            {
                switch (index)
                {
                    case 0: => obj.X = (int)value!; break;
                    case 1: => obj.Y = (string)value!; break;
                    case 2: => obj.Z = (double?)value!; break;
                    default: base[obj, index] = value; break;
                };
            }
        }
        public override bool IsNullable(int index) => index switch
        {
            2 => true,
            0 or 1 => false,
            _ => base.IsNullable(index)
        };
        public override global::System.Type GetType(int index) => index switch
        {
            0 => typeof(int),
            1 => typeof(string),
            2 => typeof(double?),
            _ => base.GetType(index)
        };
        public override TValue GetValue<TValue>(Foo.Customer obj, int index) => index switch
        {
            0 when typeof(TValue) == typeof(int) => UnsafePun<int, TValue>(obj.X),
            1 when typeof(TValue) == typeof(string) => UnsafePun<string, TValue>(obj.Y),
            2 when typeof(TValue) == typeof(double?) => UnsafePun<double?, TValue>(obj.Z),
            _ => base.GetValue<TValue>(obj, index)
        };
        public override void SetValue<TValue>(Foo.Customer obj, int index, TValue value)
        {
            switch (index)
            {
                case 0 when typeof(TValue) == typeof(int):
                    obj.X = UnsafePun<TValue, int>(value);
                    break;
                case 1 when typeof(TValue) == typeof(string):
                    obj.Y = UnsafePun<TValue, string>(value);
                    break;
                case 2 when typeof(TValue) == typeof(double?):
                    obj.Z = UnsafePun<TValue, double?>(value);
                    break;

            }

        }

    }

}
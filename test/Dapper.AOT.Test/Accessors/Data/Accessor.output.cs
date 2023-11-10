#nullable enable
#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperTypeAccessorGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Accessors\\Data\\Accessor.input.cs", 11, 26)]
        internal static global::Dapper.ObjectAccessor<global::Foo.Customer> Forwarded0(global::Foo.Customer obj, global::Dapper.TypeAccessor<global::Foo.Customer>? accessor)
            => global::Dapper.TypeAccessor.CreateAccessor(obj, accessor ?? DapperCustomTypeAccessor0.Instance);

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Accessors\\Data\\Accessor.input.cs", 14, 26)]
        internal static global::System.Data.Common.DbDataReader Forwarded1(global::System.Collections.Generic.IEnumerable<global::Foo.Customer> source, string[]? members, bool exact, global::Dapper.TypeAccessor<global::Foo.Customer>? accessor)
            => global::Dapper.TypeAccessor.CreateDataReader(source, members, exact, accessor ?? DapperCustomTypeAccessor0.Instance);

        private sealed class DapperCustomTypeAccessor0 : global::Dapper.TypeAccessor<global::Foo.Customer>
        {
            internal static readonly DapperCustomTypeAccessor0 Instance = new();
            public override int MemberCount => 6;
            public override int? TryIndex(string name, bool exact = false)
            {
                if (exact)
                {
                    return name switch
                    {
                        nameof(global::Foo.Customer.X) => 0,
                        nameof(global::Foo.Customer.Y) => 1,
                        nameof(global::Foo.Customer.Z) => 2,
                        nameof(global::Foo.Customer.State) => 3,
                        nameof(global::Foo.Customer.Obj) => 4,
                        nameof(global::Foo.Customer.DbNullProp) => 5,
                        _ => base.TryIndex(name, exact)
                    };
                }
                else
                {
                    return NormalizedHash(name) switch
                    {
                        4245442695U when NormalizedEquals(name, "x") => 0,
                        4228665076U when NormalizedEquals(name, "y") => 1,
                        4278997933U when NormalizedEquals(name, "z") => 2,
                        2016490230U when NormalizedEquals(name, "state") => 3,
                        3343205242U when NormalizedEquals(name, "obj") => 4,
                        492569847U when NormalizedEquals(name, "dbnullprop") => 5,
                        _ => base.TryIndex(name, exact)
                    };
                }
            }
            public override string GetName(int index) => index switch
            {
                0 => nameof(global::Foo.Customer.X),
                1 => nameof(global::Foo.Customer.Y),
                2 => nameof(global::Foo.Customer.Z),
                3 => nameof(global::Foo.Customer.State),
                4 => nameof(global::Foo.Customer.Obj),
                5 => nameof(global::Foo.Customer.DbNullProp),
                _ => base.GetName(index)
            };
            public override object? this[global::Foo.Customer obj, int index]
            {
                get => index switch
                {
                    0 => obj.X,
                    1 => obj.Y,
                    2 => obj.Z,
                    3 => obj.State,
                    4 => obj.Obj,
                    5 => obj.DbNullProp,
                    _ => base[obj, index]
                };
                set
                {
                    switch (index)
                    {
                        case 0: obj.X = (int)value!; break;
                        case 1: obj.Y = (string)value!; break;
                        case 2: obj.Z = (double?)value!; break;
                        case 3: obj.State = (Foo.State)value!; break;
                        case 4: obj.Obj = (object)value!; break;
                        case 5: obj.DbNullProp = (System.DBNull)value!; break;
                        default: base[obj, index] = value; break;
                    };
                }
            }
            public override bool IsNullable(int index) => index switch
            {
                2 or 4 or 5 => true,
                0 or 1 or 3 => false,
                _ => base.IsNullable(index)
            };
            public override bool IsNull(global::Foo.Customer obj, int index) => index switch
            {
                0 or 1 or 3 => false,
                2 => obj.Z is null,
                4 => obj.Obj is null or global::System.DBNull,
                5 => true,
                _ => base.IsNull(obj, index)
            };
            public override global::System.Type GetType(int index) => index switch
            {
                0 => typeof(int),
                1 => typeof(string),
                2 => typeof(double?),
                3 => typeof(Foo.State),
                4 => typeof(object),
                5 => typeof(System.DBNull),
                _ => base.GetType(index)
            };
            public override TValue GetValue<TValue>(global::Foo.Customer obj, int index) => index switch
            {
                0 when typeof(TValue) == typeof(int) => UnsafePun<int, TValue>(obj.X),
                1 when typeof(TValue) == typeof(string) => UnsafePun<string, TValue>(obj.Y),
                2 when typeof(TValue) == typeof(double?) => UnsafePun<double?, TValue>(obj.Z),
                3 when typeof(TValue) == typeof(Foo.State) || typeof(TValue) == typeof(int) => UnsafePun<Foo.State, TValue>(obj.State),
                4 when typeof(TValue) == typeof(object) => UnsafePun<object, TValue>(obj.Obj),
                5 when typeof(TValue) == typeof(System.DBNull) => UnsafePun<System.DBNull, TValue>(obj.DbNullProp),
                _ => base.GetValue<TValue>(obj, index)
            };
            public override void SetValue<TValue>(global::Foo.Customer obj, int index, TValue value)
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
                    case 3 when typeof(TValue) == typeof(Foo.State) || typeof(TValue) == typeof(int):
                        obj.State = UnsafePun<TValue, Foo.State>(value);
                        break;
                    case 4 when typeof(TValue) == typeof(object):
                        obj.Obj = UnsafePun<TValue, object>(value);
                        break;
                    case 5 when typeof(TValue) == typeof(System.DBNull):
                        obj.DbNullProp = UnsafePun<TValue, System.DBNull>(value);
                        break;

                }

            }

        }

    }
}
namespace System.Runtime.CompilerServices
{
    // this type is needed by the compiler to implement interceptors - it doesn't need to
    // come from the runtime itself, though

    [global::System.Diagnostics.Conditional("DEBUG")] // not needed post-build, so: evaporate
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : global::System.Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}
#nullable enable
file static class DapperTypeAccessorGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Accessors\\Data\\Accessor.input.cs", 13, 26)]
    public static ObjectAccessor<T> CreateReader<T>(T obj, [DapperAot] TypeAccessor<T>? accessor = null)
    {
        return DapperCustomTypeAccessor0.Instance;
    }

    private sealed class DapperCustomTypeAccessor0
    {
        internal static readonly DapperCustomTypeAccessor0 Instance = new();

    }

}
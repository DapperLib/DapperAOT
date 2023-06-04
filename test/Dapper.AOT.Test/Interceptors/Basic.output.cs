
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 13, 13)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 14, 19)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 16, 19)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 17, 25)]
    internal static global::System.Threading.Tasks.Task<int> ExecuteAsync1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Async
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 19, 19)]
    internal static global::System.Threading.Tasks.Task<int> ExecuteAsync2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Async, HasParameters
        // takes parameter: global::Foo.Customer
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 21, 13)]
    internal static global::System.Collections.Generic.IEnumerable<int> Query3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, Buffered
        // returns data: int
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 22, 19)]
    internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<int>> QueryAsync4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, Buffered
        // returns data: int
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 24, 19)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 25, 19)]
    internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<int>> QueryAsync5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, HasParameters, Buffered
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: int
        throw new global::System.NotImplementedException("lower your expectations");
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 27, 13)]
    internal static global::Foo.Customer QueryFirst6(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, Buffered, First
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        throw new global::System.NotImplementedException("lower your expectations");
    }


}
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}

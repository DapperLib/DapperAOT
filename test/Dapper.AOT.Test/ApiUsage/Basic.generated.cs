#nullable enable
using System;

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
    internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<int>> QueryAsync5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);

        // Query, Async, TypedResult, HasParameters, Buffered
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: int
#pragma warning disable CS0618
        return global::Dapper.Internal.InterceptorHelpers.QueryBufferedAsync(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn),
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
#pragma warning restore CS0618
            param, sql, commandTimeout,
            global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SequentialAccess,
            static () => new { Foo = default(int), bar = default(string) },
            static args => (args.Foo, args.bar),
            static (cmd, args) =>
            {
                cmd.CommandType = global::System.Data.CommandType.Text;
                var p = cmd.CreateParameter();
                p.ParameterName = nameof(args.Foo);
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = args.Foo;
                cmd.Parameters.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = nameof(args.bar);
                p.DbType = global::System.Data.DbType.String;
                p.Value = args.bar ?? (object)DBNull.Value;
                cmd.Parameters.Add(p);
            },
            (reader, tokens, fieldOffset) => { },
            (reader, tokens, fieldOffset) => reader.GetInt32(fieldOffset),
            global::System.Threading.CancellationToken.None
        );
    }


}

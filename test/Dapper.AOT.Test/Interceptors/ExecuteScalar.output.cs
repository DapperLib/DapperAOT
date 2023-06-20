#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 17, 24)]
    internal static float ExecuteScalar0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, TypedResult, HasParameters, StoredProcedure, Scalar
        // takes parameter: <anonymous type: int Foo, string bar>
        // parameter map: (everything)
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).ExecuteScalar<float>(param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 18, 24)]
    internal static float ExecuteScalar1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, TypedResult, StoredProcedure, Scalar
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, DefaultCommandFactory).ExecuteScalar<float>(param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 19, 24)]
    internal static float ExecuteScalar2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, TypedResult, Text, Scalar
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, DefaultCommandFactory).ExecuteScalar<float>(param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 25, 30)]
    internal static global::System.Threading.Tasks.Task<float> ExecuteScalarAsync3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Async, TypedResult, HasParameters, StoredProcedure, Scalar
        // takes parameter: <anonymous type: int Foo, string bar>
        // parameter map: (everything)
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).ExecuteScalarAsync<float>(param, default);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 26, 30)]
    internal static global::System.Threading.Tasks.Task<float> ExecuteScalarAsync4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Async, TypedResult, StoredProcedure, Scalar
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, DefaultCommandFactory).ExecuteScalarAsync<float>(param, default);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteScalar.input.cs", 27, 30)]
    internal static global::System.Threading.Tasks.Task<float> ExecuteScalarAsync5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Async, TypedResult, Text, Scalar
        // returns data: float
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, DefaultCommandFactory).ExecuteScalarAsync<float>(param, default);

    }

    private class CommonCommandFactory<T> : global::Dapper.CommandFactory<T>
    {
        public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)
        {
            var cmd = base.GetCommand(connection, sql, commandType, args);
            // apply special per-provider command initialization logic for OracleCommand
            if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
            {
                cmd0.BindByName = true;
                cmd0.InitialLONGFetchSize = -1;

            }
            return cmd;
        }

    }

    private static readonly CommonCommandFactory<object?> DefaultCommandFactory = new();

    private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
    {
        internal static readonly CommandFactory0 Instance = new();
        private CommandFactory0() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "Foo";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.Foo);
            ps.Add(p);

            p = cmd.CreateParameter();
            p.ParameterName = "bar";
            p.DbType = global::System.Data.DbType.String;
            p.Size = -1;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.bar);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(typed.Foo);
            ps[1].Value = AsValue(typed.bar);

        }
        public override bool CanPrepare => true;

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

#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Execute.input.cs", 10, 24)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: <anonymous type: int Foo, string bar>
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Execute.input.cs", 11, 24)]
    internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, StoredProcedure
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, DefaultCommandFactory).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Execute.input.cs", 12, 24)]
    internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, Text
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.Text, commandTimeout ?? -1, DefaultCommandFactory).Execute();

    }

    private class CommonCommandFactory<T> : global::Dapper.CommandFactory<T>
    {
        public override global::System.Data.Common.DbCommand Prepare(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)
        {
            var cmd = base.Prepare(connection, sql, commandType, args);
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
        public override global::System.Data.Common.DbCommand Prepare(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, object? args)
        {
            var cmd = base.Prepare(connection, sql, commandType, args);
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            global::System.Data.Common.DbParameter p;
            if (Include(sql, commandType, "Foo"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(typed.Foo);
                cmd.Parameters.Add(p);
            }
            if (Include(sql, commandType, "bar"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "bar";
                p.DbType = global::System.Data.DbType.String;
                p.Value = AsValue(typed.bar);
                cmd.Parameters.Add(p);
            }
            return cmd;
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

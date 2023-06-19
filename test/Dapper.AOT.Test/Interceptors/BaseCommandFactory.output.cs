#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\BaseCommandFactory.input.cs", 14, 24)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, StoredProcedure
        // takes parameter: <anonymous type: int Foo, string bar>
        // parameter map: (everything)
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).Execute();

    }

    private class CommonCommandFactory<T> : global::SomethingAwkward.MyCommandFactory<T>
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
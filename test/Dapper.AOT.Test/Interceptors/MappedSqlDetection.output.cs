#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 14, 20)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: A
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, (global::Foo.SomeArg)param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 17, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 23, 20)]
    internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, (global::Foo.SomeArg)param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory1.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 20, 20)]
    internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: B
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, (global::Foo.SomeArg)param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory2.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 26, 20)]
    internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: D
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, (global::Foo.SomeArg)param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory3.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 29, 20)]
    internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: E
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, (global::Foo.SomeArg)param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory4.Instance).Execute();

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

    private sealed class CommandFactory0 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory0 Instance = new();
        private CommandFactory0() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "A";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.A);
            cmd.Parameters.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            ps["A"].Value = AsValue(args.A);

        }

    }

    private sealed class CommandFactory1 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory1 Instance = new();
        private CommandFactory1() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var sql = cmd.CommandText;

        }

    }

    private sealed class CommandFactory2 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory2 Instance = new();
        private CommandFactory2() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "f";
            p.DbType = global::System.Data.DbType.String;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.B);
            cmd.Parameters.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            ps["f"].Value = AsValue(args.B);

        }

    }

    private sealed class CommandFactory3 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory3 Instance = new();
        private CommandFactory3() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "D";
            p.DbType = global::System.Data.DbType.String;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.D);
            cmd.Parameters.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            ps["D"].Value = AsValue(args.D);

        }

    }

    private sealed class CommandFactory4 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory4 Instance = new();
        private CommandFactory4() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "E";
            p.DbType = global::System.Data.DbType.String;
            p.Direction = global::System.Data.ParameterDirection.ReturnValue;
            p.Value = global::System.DBNull.Value;
            cmd.Parameters.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            ps["E"].Value = global::System.DBNull.Value;

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
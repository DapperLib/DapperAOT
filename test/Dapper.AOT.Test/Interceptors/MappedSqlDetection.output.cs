#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 14, 20)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: A
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::Foo.SomeArg)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 17, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 23, 20)]
    internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory1.Instance).Execute((global::Foo.SomeArg)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 20, 20)]
    internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: B
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory2.Instance).Execute((global::Foo.SomeArg)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 26, 20)]
    internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: D
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory3.Instance).Execute((global::Foo.SomeArg)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 29, 20)]
    internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeArg
        // parameter map: E
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory4.Instance).Execute((global::Foo.SomeArg)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MappedSqlDetection.input.cs", 32, 20)]
    internal static int Execute5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::Foo.SomeOtherArg
        // parameter map: X
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::Foo.SomeOtherArg>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory5.Instance).Execute((global::Foo.SomeOtherArg)param);

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
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "A";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.A);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(args.A);

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args, int rowCount)
        {
            args.RowCount = rowCount;
            PostProcess(cmd, args);
        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory1 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory1 Instance = new();
        private CommandFactory1() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args, int rowCount)
        {
            args.RowCount = rowCount;
            PostProcess(cmd, args);
        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory2 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory2 Instance = new();
        private CommandFactory2() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "f";
            p.DbType = global::System.Data.DbType.String;
            p.Size = 200;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.B);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(args.B);

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args, int rowCount)
        {
            args.RowCount = rowCount;
            PostProcess(cmd, args);
        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory3 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory3 Instance = new();
        private CommandFactory3() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "D";
            p.DbType = global::System.Data.DbType.Decimal;
            p.Precision = 12;
            p.Scale = 42;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.D);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(args.D);

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args, int rowCount)
        {
            args.RowCount = rowCount;
            PostProcess(cmd, args);
        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory4 : CommonCommandFactory<global::Foo.SomeArg>
    {
        internal static readonly CommandFactory4 Instance = new();
        private CommandFactory4() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "E";
            p.DbType = global::System.Data.DbType.String;
            p.Size = -1;
            p.Direction = global::System.Data.ParameterDirection.ReturnValue;
            p.Value = global::System.DBNull.Value;
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            ps[0].Value = global::System.DBNull.Value;

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args)
        {
            var ps = cmd.Parameters;
            args.E = Parse<string>(ps[0].Value);

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeArg args, int rowCount)
        {
            args.RowCount = rowCount;
            PostProcess(cmd, args);
        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory5 : CommonCommandFactory<global::Foo.SomeOtherArg>
    {
        internal static readonly CommandFactory5 Instance = new();
        private CommandFactory5() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeOtherArg args)
        {
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "X";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(args.X);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.SomeOtherArg args)
        {
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(args.X);

        }
        public override void PostProcess(global::System.Data.Common.DbCommand cmd, global::Foo.SomeOtherArg args, int rowCount)
        {
            args.RowCount = rowCount;
            args.DuplicateToCauseProblems = rowCount;
            PostProcess(cmd, args);
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
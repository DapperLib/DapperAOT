#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 12, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 14, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 16, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 18, 20)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 20, 20)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: <anonymous type: int a, string b>
        // parameter map: a b
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute(param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 23, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 26, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 29, 24)]
    internal static dynamic QuerySingle1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, SingleRow, Text, AtLeastOne, AtMostOne
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 32, 24)]
    internal static dynamic QuerySingle2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne
        // takes parameter: <anonymous type: int[] ids>
        // parameter map: ids
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 35, 24)]
    internal static dynamic QuerySingle3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne
        // takes parameter: <anonymous type: int a, string b>
        // parameter map: a
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 38, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 41, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 42, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 45, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 48, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 55, 24)]
    internal static global::System.Collections.Generic.IEnumerable<dynamic> Query4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Buffered, Text
        global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
        global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(buffered is true);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

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

    private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int a, string b>
    {
        internal static readonly CommandFactory0 Instance = new();
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "a";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.a);
            ps.Add(p);

            p = cmd.CreateParameter();
            p.ParameterName = "b";
            p.DbType = global::System.Data.DbType.String;
            p.Size = -1;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.b);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(typed.a);
            ps[1].Value = AsValue(typed.b);

        }
        public override bool CanPrepare => true;

    }

    private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int[] ids>
    {
        internal static readonly CommandFactory1 Instance = new();
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "ids";
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.ids);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(typed.ids);

        }

    }

    private sealed class CommandFactory2 : CommonCommandFactory<object?> // <anonymous type: int a, string b>
    {
        internal static readonly CommandFactory2 Instance = new();
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            global::System.Data.Common.DbParameter p;
            p = cmd.CreateParameter();
            p.ParameterName = "a";
            p.DbType = global::System.Data.DbType.Int32;
            p.Direction = global::System.Data.ParameterDirection.Input;
            p.Value = AsValue(typed.a);
            ps.Add(p);

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            ps[0].Value = AsValue(typed.a);

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
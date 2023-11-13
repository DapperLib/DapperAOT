#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 14, 20)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: int A, int B, int C>
            // parameter map: B
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 17, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 28, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 31, 20)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::<anonymous type: int A, int B, int C>[]
            // parameter map: B
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((object?[])param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 20, 20)]
        internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, Text
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 23, 20)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: int A, int B, int C>
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\SqlDetection.input.cs", 34, 20)]
        internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::<anonymous type: int A, int B, int C>[]
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).Execute((object?[])param!);

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int A, int B, int C>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { A = default(int), B = default(int), C = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "B";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.B);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { A = default(int), B = default(int), C = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.B);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override bool CanPrepare => true;

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
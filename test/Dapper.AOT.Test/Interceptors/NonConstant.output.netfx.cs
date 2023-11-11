#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\NonConstant.input.cs", 17, 20)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, KnownParameters
            // takes parameter: <anonymous type: int A, int B, int C>
            // parameter map: (deferred)
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, default, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\NonConstant.input.cs", 19, 20)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, KnownParameters
            // takes parameter: global::<anonymous type: int A, int B, int C>[]
            // parameter map: (deferred)
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, default, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((object?[])param!);

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
                var sql = cmd.CommandText;
                var commandType = cmd.CommandType;
                if (Include(sql, commandType, "A"))
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "A";
                    p.DbType = global::System.Data.DbType.Int32;
                    p.Direction = global::System.Data.ParameterDirection.Input;
                    p.Value = AsValue(typed.A);
                    ps.Add(p);

                }

                if (Include(sql, commandType, "B"))
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "B";
                    p.DbType = global::System.Data.DbType.Int32;
                    p.Direction = global::System.Data.ParameterDirection.Input;
                    p.Value = AsValue(typed.B);
                    ps.Add(p);

                }

                if (Include(sql, commandType, "C"))
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "C";
                    p.DbType = global::System.Data.DbType.Int32;
                    p.Direction = global::System.Data.ParameterDirection.Input;
                    p.Value = AsValue(typed.C);
                    ps.Add(p);

                }

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { A = default(int), B = default(int), C = default(int) }); // expected shape
                var ps = cmd.Parameters;
                var sql = cmd.CommandText;
                var commandType = cmd.CommandType;
                if (Include(sql, commandType, "A"))
                {
                    ps["A"].Value = AsValue(typed.A);

                }
                if (Include(sql, commandType, "B"))
                {
                    ps["B"].Value = AsValue(typed.B);

                }
                if (Include(sql, commandType, "C"))
                {
                    ps["C"].Value = AsValue(typed.C);

                }

            }
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
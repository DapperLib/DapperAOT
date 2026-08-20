#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\LiteralTokens.input.cs", 9, 23)]
        internal static int QuerySingle0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne, KnownParameters
            // takes parameter: <anonymous type: int Admin, int a>
            // parameter map: a
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int Admin, int a>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Admin = default(int), a = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "a";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.a);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Admin = default(int), a = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.a);

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
namespace Dapper.Aot.Generated
{
    // installs the runtime type-handler bridge: SqlMapper.AddTypeHandler registrations reach
    // Dapper.AOT's readers through these callbacks, compiled against *this* project's Dapper
    // (which may be Dapper or Dapper.StrongName - the library cannot reference either)
    file static class TypeHandlerBridgeInitializer
    {
        [global::System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize() => global::Dapper.TypeHandlerBridge.Configure(
            static type => global::Dapper.SqlMapper.HasTypeHandler(type),
            static (type, value) =>
            {
#pragma warning disable CS0618 // vanilla's decision procedure: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(type, "", false, out var handler);
#pragma warning restore CS0618
                return handler is null ? value : handler.Parse(type, value);
            });
    }
}

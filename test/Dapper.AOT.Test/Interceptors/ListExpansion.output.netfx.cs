#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ListExpansion.input.cs", 14, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: int[] ids>
            // parameter map: ids
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ListExpansion.input.cs", 15, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: List<int> ids, string region>
            // parameter map: ids region
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ListExpansion.input.cs", 16, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: string[] names>
            // parameter map: names
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int[] ids>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(typeof(int[]), "ids", false, out var typeHandlerids);
                // a runtime type-handler for the collection type wins over expansion,
                // which is the order vanilla's own decision procedure applies
                if (typeHandlerids is not null)
                {
                    var hp = cmd.CreateParameter();
                    hp.ParameterName = "ids";
                    hp.Direction = global::System.Data.ParameterDirection.Input;
                    typeHandlerids.SetValue(hp, (object?)typed.ids ?? global::System.DBNull.Value);
                    ps.Add(hp);
                }
                else
                {
                    global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "ids", typed.ids);
                }
                #pragma warning restore CS0618

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;

            }

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: List<int> ids, string region>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(global::System.Collections.Generic.List<int>)!, region = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(typeof(global::System.Collections.Generic.List<int>), "ids", false, out var typeHandlerids);
                // a runtime type-handler for the collection type wins over expansion,
                // which is the order vanilla's own decision procedure applies
                if (typeHandlerids is not null)
                {
                    var hp = cmd.CreateParameter();
                    hp.ParameterName = "ids";
                    hp.Direction = global::System.Data.ParameterDirection.Input;
                    typeHandlerids.SetValue(hp, (object?)typed.ids ?? global::System.DBNull.Value);
                    ps.Add(hp);
                }
                else
                {
                    global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "ids", typed.ids);
                }
                #pragma warning restore CS0618

                p = cmd.CreateParameter();
                p.ParameterName = "region";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.region);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(global::System.Collections.Generic.List<int>)!, region = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[1].Value = AsValue(typed.region);

            }

        }

        private sealed class CommandFactory2 : CommonCommandFactory<object?> // <anonymous type: string[] names>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { names = default(string[])! }); // expected shape
                var ps = cmd.Parameters;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(typeof(string[]), "names", false, out var typeHandlernames);
                // a runtime type-handler for the collection type wins over expansion,
                // which is the order vanilla's own decision procedure applies
                if (typeHandlernames is not null)
                {
                    var hp = cmd.CreateParameter();
                    hp.ParameterName = "names";
                    hp.Direction = global::System.Data.ParameterDirection.Input;
                    typeHandlernames.SetValue(hp, (object?)typed.names ?? global::System.DBNull.Value);
                    ps.Add(hp);
                }
                else
                {
                    global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "names", typed.names);
                }
                #pragma warning restore CS0618

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { names = default(string[])! }); // expected shape
                var ps = cmd.Parameters;

            }

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
namespace System.Runtime.CompilerServices
{
    // down-level polyfill; the compiler matches this attribute by full name
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
    internal sealed class ModuleInitializerAttribute : global::System.Attribute { }
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

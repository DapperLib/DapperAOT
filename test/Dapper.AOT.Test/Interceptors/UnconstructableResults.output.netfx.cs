#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\UnconstructableResults.input.cs", 17, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Poco> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.Poco
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory0.Instance);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.Poco>
        {
            internal static readonly RowFactory0 Instance = new();
            private RowFactory0() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 1113510858U when NormalizedEquals(name, "value"):
                            token = type == typeof(int) ? 0 : 1; // two tokens for right-typed and type-flexible
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Foo.Poco Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Poco result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Value = reader.GetInt32(columnOffset);
                            break;
                        case 1:
                            result.Value = GetValue<int>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

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

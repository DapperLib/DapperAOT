#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerDispatch.input.cs", 14, 24)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::Foo.EventRow
            // parameter map: At Name
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::Foo.EventRow)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TypeHandlerDispatch.input.cs", 17, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.EventRow> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, Text, BindResultsByName
            // returns data: global::Foo.EventRow
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory0.Instance);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.EventRow>
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
                        case 1462048136U when NormalizedEquals(name, "at"):
                            token = type == typeof(global::Foo.LocalDate) ? 0 : 2; // two tokens for right-typed and type-flexible
                            break;
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 1 : 3;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Foo.EventRow Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.EventRow result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.At = reader.IsDBNull(columnOffset) ? (global::Foo.LocalDate?)null : reader.GetFieldValue<global::Foo.LocalDate>(columnOffset);
                            break;
                        case 2:
                            result.At = reader.IsDBNull(columnOffset) ? (global::Foo.LocalDate?)null : GetValue<global::Foo.LocalDate>(reader, columnOffset);
                            break;
                        case 1:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 3:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<global::Foo.EventRow>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.EventRow args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                #pragma warning disable CS0618 // vanilla's decision procedure: this *is* the library usage
                var dbTypeAt = global::Dapper.SqlMapper.LookupDbType(typeof(global::Foo.LocalDate), "At", false, out var typeHandlerAt);
                #pragma warning restore CS0618
                p = cmd.CreateParameter();
                p.ParameterName = "At";
                p.Direction = global::System.Data.ParameterDirection.Input;
                if (typeHandlerAt is not null)
                {
                    typeHandlerAt.SetValue(p, (object?)args.At ?? global::System.DBNull.Value);
                }
                else
                {
                    if (dbTypeAt is not null) p.DbType = dbTypeAt.GetValueOrDefault();
                    p.Value = AsValue(args.At);

                }
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Name";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, args.Name);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.EventRow args)
            {
                var ps = cmd.Parameters;
                #pragma warning disable CS0618 // vanilla's decision procedure: this *is* the library usage
                _ = global::Dapper.SqlMapper.LookupDbType(typeof(global::Foo.LocalDate), "At", false, out var typeHandlerAt);
                #pragma warning restore CS0618
                if (typeHandlerAt is not null)
                {
                    typeHandlerAt.SetValue(ps[0], (object?)args.At ?? global::System.DBNull.Value);
                }
                else
                {
                    ps[0].Value = AsValue(args.At);
                }
                ps[1].Value = AsValue(args.Name);

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

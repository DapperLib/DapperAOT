#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\DateOnly.net6.input.cs", 12, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.User>> QueryAsync0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: DateOnly BirthDate>
            // parameter map: BirthDate
            // returns data: global::Foo.User
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryBufferedAsync(param, RowFactory0.Instance));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\DateOnly.net6.input.cs", 17, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.User>> QueryAsync1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: global::Foo.QueryModel
            // parameter map: BirthDate
            // returns data: global::Foo.User
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).QueryBufferedAsync((global::Foo.QueryModel)param!, RowFactory0.Instance));

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.User>
        {
            internal static readonly RowFactory0 Instance = new();
            private RowFactory0() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                int availableColumns = reader.FieldCount - columnOffset;
                int tokenCount = global::System.Math.Min(tokens.Length, availableColumns);
                for (int i = 0; i < tokenCount; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 926444256U when NormalizedEquals(name, "id"):
                            token = type == typeof(int) ? 0 : 3; // two tokens for right-typed and type-flexible
                            break;
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 1 : 4;
                            break;
                        case 4237030186U when NormalizedEquals(name, "birthdate"):
                            token = type == typeof(global::System.DateOnly) ? 2 : 5;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                // Initialize remaining tokens to -1 (unmapped)
                for (int i = tokenCount; i < tokens.Length; i++)
                {
                    tokens[i] = -1;
                }
                return tokenCount;
            }
            public override global::Foo.User Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                if (columnOffset > 0 && reader.IsDBNull(columnOffset)) return default!;
                global::Foo.User result = new();
                int tokenCount = state is int count ? count : tokens.Length;
                for (int i = 0; i < tokenCount; i++)
                {
                    var token = tokens[i];
                    switch (token)
                    {
                        case 0:
                            result.Id = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            result.Id = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            result.BirthDate = reader.GetFieldValue<global::System.DateOnly>(columnOffset);
                            break;
                        case 5:
                            result.BirthDate = GetValue<global::System.DateOnly>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: DateOnly BirthDate>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { BirthDate = default(global::System.DateOnly) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "BirthDate";
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.BirthDate);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { BirthDate = default(global::System.DateOnly) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.BirthDate);

            }

        }

        private sealed class CommandFactory1 : CommonCommandFactory<global::Foo.QueryModel>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.QueryModel args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "BirthDate";
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.BirthDate);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.QueryModel args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.BirthDate);

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
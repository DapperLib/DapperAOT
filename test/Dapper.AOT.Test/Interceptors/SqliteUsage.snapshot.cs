#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(@"/_/test/Dapper.AOT.Test/Interceptors/SqliteUsage.input.cs", 11, 23)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: string name, SomeEnum value>
            // parameter map: name value
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(@"/_/test/Dapper.AOT.Test/Interceptors/SqliteUsage.input.cs", 14, 23)]
        internal static global::System.Collections.Generic.IEnumerable<global::Dapper.AOT.Test.Integration.TypeWithSomeEnum> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, Text, BindResultsByName
            // returns data: global::Dapper.AOT.Test.Integration.TypeWithSomeEnum
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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Dapper.AOT.Test.Integration.TypeWithSomeEnum>
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
                        case 926444256U when NormalizedEquals(name, "id"):
                            token = type == typeof(int) ? 0 : 3; // two tokens for right-typed and type-flexible
                            break;
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 1 : 4;
                            break;
                        case 1113510858U when NormalizedEquals(name, "value"):
                            token = type == typeof(global::Dapper.AOT.Test.Integration.SomeEnum) ? 2 : 5;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Dapper.AOT.Test.Integration.TypeWithSomeEnum Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Dapper.AOT.Test.Integration.TypeWithSomeEnum result = new();
                foreach (var token in tokens)
                {
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
                            result.Value = reader.GetFieldValue<global::Dapper.AOT.Test.Integration.SomeEnum>(columnOffset);
                            break;
                        case 5:
                            result.Value = GetValue<global::Dapper.AOT.Test.Integration.SomeEnum>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: string name, SomeEnum value>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { name = default(string)!, value = default(global::Dapper.AOT.Test.Integration.SomeEnum) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "name";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.name);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "value";
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.value);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { name = default(string)!, value = default(global::Dapper.AOT.Test.Integration.SomeEnum) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.name);
                ps[1].Value = AsValue(typed.value);

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
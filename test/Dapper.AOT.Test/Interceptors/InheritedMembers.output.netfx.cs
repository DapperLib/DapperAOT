#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\InheritedMembers.input.cs", 11, 30)]
        internal static global::Entity1 QueryFirst0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, SingleRow, StoredProcedure, AtLeastOne, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int Foo, string bar>
            // parameter map: bar Foo
            // returns data: global::Entity1
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryFirst(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\InheritedMembers.input.cs", 12, 20)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::Entity1
            // parameter map: Id Name
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).Execute((global::Entity1)param!);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Entity1>
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
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 0 : 2; // two tokens for right-typed and type-flexible
                            break;
                        case 926444256U when NormalizedEquals(name, "id"):
                            token = type == typeof(long) ? 1 : 3;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Entity1 Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Entity1 result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 2:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 1:
                            result.Id = reader.GetInt64(columnOffset);
                            break;
                        case 3:
                            result.Id = GetValue<long>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.Foo);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "bar";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.bar);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.Foo);
                ps[1].Value = AsValue(typed.bar);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<global::Entity1>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Entity1 args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Name";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, args.Name);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int64;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.Id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Entity1 args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.Name);
                ps[1].Value = AsValue(args.Id);

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
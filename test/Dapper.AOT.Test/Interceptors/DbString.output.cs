#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\DbString.input.cs", 11, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Product>> QueryAsync0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: DbString Name, int Id>
            // parameter map: Id Name
            // returns data: global::Foo.Product
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryBufferedAsync(param, RowFactory0.Instance));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\DbString.input.cs", 24, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Product>> QueryAsync1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, HasParameters, Buffered, Text, BindResultsByName, KnownParameters
            // takes parameter: global::Foo.QueryModel
            // parameter map: Id Name
            // returns data: global::Foo.Product
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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.Product>
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
                        case 2521315361U when NormalizedEquals(name, "productid"):
                            token = type == typeof(int) ? 0 : 3; // two tokens for right-typed and type-flexible
                            break;
                        case 2369371622U when NormalizedEquals(name, "name"):
                            token = type == typeof(string) ? 1 : 4;
                            break;
                        case 1133313085U when NormalizedEquals(name, "productnumber"):
                            token = type == typeof(string) ? 2 : 5;
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Foo.Product Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Product result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.ProductId = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            result.ProductId = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            result.Name = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            result.ProductNumber = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 5:
                            result.ProductNumber = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: DbString Name, int Id>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Name = default(global::Dapper.DbString)!, Id = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Name";
                global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(p, typed.Name);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.Id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Name = default(global::Dapper.DbString)!, Id = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(ps[0], typed.Name);
                ps[1].Value = AsValue(typed.Id);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<global::Foo.QueryModel>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.QueryModel args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Name";
                global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(p, args.Name);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.Id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.QueryModel args)
            {
                var ps = cmd.Parameters;
                global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(ps[0], args.Name);
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
namespace Dapper.Aot.Generated
{
    /// <summary>
    /// Contains helpers to properly handle <see href="https://github.com/DapperLib/Dapper/blob/main/Dapper/DbString.cs"/>
    /// </summary>
#if !DAPPERAOT_INTERNAL
    file
#endif
    static class DbStringHelpers
    {
        public static void ConfigureDbStringDbParameter(
            global::System.Data.Common.DbParameter dbParameter,
            global::Dapper.DbString? dbString)
        {
            if (dbString is null)
            {
                dbParameter.Value = global::System.DBNull.Value;
                return;
            }

            // repeating logic from Dapper:
            // https://github.com/DapperLib/Dapper/blob/52160dc44699ec7eb5ad57d0dddc6ded4662fcb9/Dapper/DbString.cs#L71
            if (dbString.Length == -1 && dbString.Value is not null && dbString.Value.Length <= global::Dapper.DbString.DefaultLength)
            {
                dbParameter.Size = global::Dapper.DbString.DefaultLength;
            }
            else
            {
                dbParameter.Size = dbString.Length;
            }

            dbParameter.DbType = dbString switch
            {
                { IsAnsi: true, IsFixedLength: true } => global::System.Data.DbType.AnsiStringFixedLength,
                { IsAnsi: true, IsFixedLength: false } => global::System.Data.DbType.AnsiString,
                { IsAnsi: false, IsFixedLength: true } => global::System.Data.DbType.StringFixedLength,
                { IsAnsi: false, IsFixedLength: false } => global::System.Data.DbType.String,
                _ => dbParameter.DbType
            };

            dbParameter.Value = dbString.Value as object ?? global::System.DBNull.Value;
        }
    }
}
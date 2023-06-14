#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 13, 24)]
    internal static global::Foo.Customer QueryFirst0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow, AtLeastOne
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).QueryFirst<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 14, 24)]
    internal static global::Foo.Customer? QueryFirstOrDefault1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow, StoredProcedure
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).QueryFirstOrDefault<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 15, 24)]
    internal static global::Foo.Customer QuerySingle2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).QuerySingle<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 16, 24)]
    internal static global::Foo.Customer? QuerySingleOrDefault3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, SingleRow, AtMostOne
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).QuerySingleOrDefault<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 18, 30)]
    internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QueryFirstAsync4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, HasParameters, SingleRow, AtLeastOne
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).QueryFirstAsync<global::Foo.Customer>(RowFactory0.Instance, default);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 19, 30)]
    internal static global::System.Threading.Tasks.Task<global::Foo.Customer>? QueryFirstOrDefaultAsync5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, HasParameters, SingleRow, StoredProcedure
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).QueryFirstOrDefaultAsync<global::Foo.Customer>(RowFactory0.Instance, default);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 20, 30)]
    internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync6(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).QuerySingleAsync<global::Foo.Customer>(RowFactory0.Instance, default);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 21, 30)]
    internal static global::System.Threading.Tasks.Task<global::Foo.Customer>? QuerySingleOrDefaultAsync7(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, Async, TypedResult, SingleRow, AtMostOne
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).QuerySingleOrDefaultAsync<global::Foo.Customer>(RowFactory0.Instance, default);

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

    private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.Customer>
    {
        internal static readonly RowFactory0 Instance = new();
        private RowFactory0() {}
        public override void Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                int token = -1;
                var name = reader.GetName(columnOffset);
                var type = reader.GetFieldType(columnOffset);
                switch (NormalizedHash(name))
                {
                    case 4245442695U when NormalizedEquals(name, "x"):
                        token = type == typeof(int) ? 0 : 3; // two tokens for right-typed and type-flexible
                        break;
                    case 4228665076U when NormalizedEquals(name, "y"):
                        token = type == typeof(string) ? 1 : 4;
                        break;
                    case 4278997933U when NormalizedEquals(name, "z"):
                        token = type == typeof(double) ? 2 : 5;
                        break;

                }
                tokens[i] = token;
                columnOffset++;

            }

        }
        public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
        {
            global::Foo.Customer result = new();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case 0:
                        result.X = reader.GetInt32(columnOffset);
                        break;
                    case 3:
                        result.X = GetValue<int>(reader, columnOffset);
                        break;
                    case 1:
                        result.Y = reader.GetString(columnOffset);
                        break;
                    case 4:
                        result.Y = GetValue<string>(reader, columnOffset);
                        break;
                    case 2:
                        result.Z = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                        break;
                    case 5:
                        result.Z = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
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
        private CommandFactory0() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            global::System.Data.Common.DbParameter p;
            // if (Include(sql, commandType, "Foo"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(typed.Foo);
                cmd.Parameters.Add(p);
            }
            // if (Include(sql, commandType, "bar"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "bar";
                p.DbType = global::System.Data.DbType.String;
                p.Value = AsValue(typed.bar);
                cmd.Parameters.Add(p);
            }

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, object? args)
        {
            var sql = cmd.CommandText;
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            var ps = cmd.Parameters;
            // if (Include(sql, commandType, "Foo"))
            {
                ps["Foo"].Value = AsValue(typed.Foo);

            }
            // if (Include(sql, commandType, "bar"))
            {
                ps["bar"].Value = AsValue(typed.bar);

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
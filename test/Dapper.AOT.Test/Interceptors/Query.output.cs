
#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Query.input.cs", 10, 24)]
    internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, Buffered
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(buffered is true);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return new global::Dapper.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).QueryBuffered<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Query.input.cs", 11, 24)]
    internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return new global::Dapper.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Query<global::Foo.Customer>(buffered, RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Query.input.cs", 12, 24)]
    internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, Unbuffered, StoredProcedure
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(buffered is false);
        global::System.Diagnostics.Debug.Assert(param is null);

        return new global::Dapper.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, DefaultCommandFactory).QueryUnbuffered<global::Foo.Customer>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Query.input.cs", 13, 24)]
    internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, Buffered, Text
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(buffered is true);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return new global::Dapper.Command<object?>(cnn, transaction, sql, param, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).QueryBuffered<global::Foo.Customer>(RowFactory0.Instance);

    }

    private class CommonCommandFactory<T> : global::Dapper.CommandFactory<T>
    {
        public override global::System.Data.Common.DbCommand Prepare(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)
        {
            var cmd = base.Prepare(connection, sql, commandType, args);
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
        public override global::System.Data.Common.DbCommand Prepare(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, object? args)
        {
            var cmd = base.Prepare(connection, sql, commandType, args);
            var typed = Cast(args, static () => new { Foo = default(int), bar = default(string)! }); // expected shape
            global::System.Data.Common.DbParameter p;
            if (Include(sql, commandType, "Foo"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(typed.Foo);
                cmd.Parameters.Add(p);
            }
            if (Include(sql, commandType, "bar"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "bar";
                p.DbType = global::System.Data.DbType.String;
                p.Value = AsValue(typed.bar);
                cmd.Parameters.Add(p);
            }
            return cmd;
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

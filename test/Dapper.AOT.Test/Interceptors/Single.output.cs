
#nullable enable
file static partial class DapperGeneratedInterceptors
{
#pragma warning disable CS0618

    // placeholder for per-provider setup rules
    static partial void InitCommand(global::System.Data.Common.DbCommand cmd);

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 10, 24)]
    internal static unsafe global::Foo.Customer QueryFirst0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.Internal.InterceptorHelpers.UnsafeQueryFirst(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
            global::Dapper.Internal.InterceptorHelpers.Reshape(param!, // transform anon-type
                static () => new { Foo = default(int), bar = default(string)! }, // expected shape
                static args => (args.Foo, args.bar) ), // project to named type
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
            commandTimeout, &CommandBuilder, &ColumnTokenizer0, &RowReader0);

        static void CommandBuilder(global::System.Data.Common.DbCommand cmd, (int Foo, string bar) args)
        {
            InitCommand(cmd);
            cmd.CommandType = global::System.Data.CommandType.Text;
            var p = cmd.CreateParameter();
            p.ParameterName = "Foo";
            p.DbType = global::System.Data.DbType.Int32;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.Foo);
            cmd.Parameters.Add(p);
            p = cmd.CreateParameter();
            p.ParameterName = "bar";
            p.DbType = global::System.Data.DbType.String;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.bar);
            cmd.Parameters.Add(p);

        }
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 11, 24)]
    internal static unsafe global::Foo.Customer QueryFirstOrDefault1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow, StoredProcedure
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.Internal.InterceptorHelpers.UnsafeQueryFirstOrDefault(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
            global::Dapper.Internal.InterceptorHelpers.Reshape(param!, // transform anon-type
                static () => new { Foo = default(int), bar = default(string)! }, // expected shape
                static args => (args.Foo, args.bar) ), // project to named type
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
            commandTimeout, &CommandBuilder, &ColumnTokenizer0, &RowReader0);

        static void CommandBuilder(global::System.Data.Common.DbCommand cmd, (int Foo, string bar) args)
        {
            InitCommand(cmd);
            cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
            var p = cmd.CreateParameter();
            p.ParameterName = "Foo";
            p.DbType = global::System.Data.DbType.Int32;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.Foo);
            cmd.Parameters.Add(p);
            p = cmd.CreateParameter();
            p.ParameterName = "bar";
            p.DbType = global::System.Data.DbType.String;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.bar);
            cmd.Parameters.Add(p);

        }
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 12, 24)]
    internal static unsafe global::Foo.Customer QuerySingle2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, SingleRow, Text
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.Internal.InterceptorHelpers.UnsafeQuerySingle(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
            global::Dapper.Internal.InterceptorHelpers.Reshape(param!, // transform anon-type
                static () => new { Foo = default(int), bar = default(string)! }, // expected shape
                static args => (args.Foo, args.bar) ), // project to named type
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
            commandTimeout, &CommandBuilder, &ColumnTokenizer0, &RowReader0);

        static void CommandBuilder(global::System.Data.Common.DbCommand cmd, (int Foo, string bar) args)
        {
            InitCommand(cmd);
            cmd.CommandType = global::System.Data.CommandType.Text;
            var p = cmd.CreateParameter();
            p.ParameterName = "Foo";
            p.DbType = global::System.Data.DbType.Int32;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.Foo);
            cmd.Parameters.Add(p);
            p = cmd.CreateParameter();
            p.ParameterName = "bar";
            p.DbType = global::System.Data.DbType.String;
            p.Value = global::Dapper.Internal.InterceptorHelpers.AsValue(args.bar);
            cmd.Parameters.Add(p);

        }
    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Single.input.cs", 13, 24)]
    internal static unsafe global::Foo.Customer QuerySingleOrDefault3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, SingleRow
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.Internal.InterceptorHelpers.UnsafeQuerySingleOrDefault(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
            (object?)null,
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
            commandTimeout, &CommandBuilder, &ColumnTokenizer0, &RowReader0);

        static void CommandBuilder(global::System.Data.Common.DbCommand cmd, object? args)
        {
            InitCommand(cmd);
            cmd.CommandType = global::System.Data.CommandType.Text;

        }
    }

    private static void ColumnTokenizer0(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int fieldOffset)
    {
        // tokenize global::Foo.Customer
        for (int i = 0; i < tokens.Length; i++)
        {
            int token = -1;
            var name = reader.GetName(fieldOffset);
            var type = reader.GetFieldType(fieldOffset);
            switch (global::Dapper.Internal.StringHashing.NormalizedHash(name))
            {
                case 4245442695U when global::Dapper.Internal.StringHashing.NormalizedEquals(name, "x"):
                    token = type == typeof(int) ? 0 : 3; // two tokens for right-typed and type-flexible
                    break;
                case 4228665076U when global::Dapper.Internal.StringHashing.NormalizedEquals(name, "y"):
                    token = type == typeof(string) ? 1 : 4;
                    break;
                case 4278997933U when global::Dapper.Internal.StringHashing.NormalizedEquals(name, "z"):
                    token = type == typeof(double) ? 2 : 5;
                    break;

            }
            tokens[i] = token;
            fieldOffset++;

        }

    }
    private static global::Foo.Customer RowReader0(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int fieldOffset)
    {
        // parse global::Foo.Customer
        global::Foo.Customer result = new();
        foreach (var token in tokens)
        {
            switch (token)
            {
                case 0:
                    result.X = reader.GetInt32(fieldOffset);
                    break;
                case 3:
                    result.X = global::Dapper.Internal.InterceptorHelpers.GetValue<int>(reader, fieldOffset);
                    break;
                case 1:
                    result.Y = reader.GetString(fieldOffset);
                    break;
                case 4:
                    result.Y = global::Dapper.Internal.InterceptorHelpers.GetValue<string>(reader, fieldOffset);
                    break;
                case 2:
                    result.Z = reader.IsDBNull(fieldOffset) ? (double?)null : reader.GetDouble(fieldOffset);
                    break;
                case 5:
                    result.Z = reader.IsDBNull(fieldOffset) ? (double?)null : global::Dapper.Internal.InterceptorHelpers.GetValue<double>(reader, fieldOffset);
                    break;

            }
            fieldOffset++;

        }
        return result;

    }
    static partial void InitCommand(global::System.Data.Common.DbCommand cmd)
    {
        // apply special per-provider command initialization logic
        if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
        {
            cmd0.BindByName = true;
            cmd0.InitialLONGFetchSize = -1;

        }
    }

#pragma warning restore CS0618
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

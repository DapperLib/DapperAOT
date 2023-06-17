#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 26, 20)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 35, 24)]
    internal static global::SomeCode.InternalNesting.SomePublicType QueryFirst1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, SingleRow, AtLeastOne
        // returns data: global::SomeCode.InternalNesting.SomePublicType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).QueryFirst<global::SomeCode.InternalNesting.SomePublicType>(RowFactory0.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 36, 24)]
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 92, 24)]
    internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::SomeCode.InternalNesting.SomePublicType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::SomeCode.InternalNesting.SomePublicType>(cnn, transaction, sql, (global::SomeCode.InternalNesting.SomePublicType)param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 39, 24)]
    internal static global::SomeCode.InternalNesting.SomeInternalType QueryFirst3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, SingleRow, AtLeastOne
        // returns data: global::SomeCode.InternalNesting.SomeInternalType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).QueryFirst<global::SomeCode.InternalNesting.SomeInternalType>(RowFactory1.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 40, 24)]
    internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::SomeCode.InternalNesting.SomeInternalType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::SomeCode.InternalNesting.SomeInternalType>(cnn, transaction, sql, (global::SomeCode.InternalNesting.SomeInternalType)param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory1.Instance).Execute();

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 55, 24)]
    internal static global::SomeCode.InternalNesting.SomeProtectedInternalType QueryFirst5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, SingleRow, AtLeastOne
        // returns data: global::SomeCode.InternalNesting.SomeProtectedInternalType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is null);

        return global::Dapper.DapperAotExtensions.Command<object?>(cnn, transaction, sql, param, commandType.GetValueOrDefault(), commandTimeout ?? -1, DefaultCommandFactory).QueryFirst<global::SomeCode.InternalNesting.SomeProtectedInternalType>(RowFactory2.Instance);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\MiscDiagnostics.input.cs", 56, 24)]
    internal static int Execute6(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::SomeCode.InternalNesting.SomeProtectedInternalType
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Command<global::SomeCode.InternalNesting.SomeProtectedInternalType>(cnn, transaction, sql, (global::SomeCode.InternalNesting.SomeProtectedInternalType)param, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory2.Instance).Execute();

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

    private sealed class RowFactory0 : global::Dapper.RowFactory<global::SomeCode.InternalNesting.SomePublicType>
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
                    case 926444256U when NormalizedEquals(name, "id"):
                        token = type == typeof(int) ? 0 : 1; // two tokens for right-typed and type-flexible
                        break;

                }
                tokens[i] = token;
                columnOffset++;

            }

        }
        public override global::SomeCode.InternalNesting.SomePublicType Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
        {
            global::SomeCode.InternalNesting.SomePublicType result = new();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case 0:
                        result.Id = reader.GetInt32(columnOffset);
                        break;
                    case 1:
                        result.Id = GetValue<int>(reader, columnOffset);
                        break;

                }
                columnOffset++;

            }
            return result;

        }

    }

    private sealed class RowFactory1 : global::Dapper.RowFactory<global::SomeCode.InternalNesting.SomeInternalType>
    {
        internal static readonly RowFactory1 Instance = new();
        private RowFactory1() {}
        public override void Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                int token = -1;
                var name = reader.GetName(columnOffset);
                var type = reader.GetFieldType(columnOffset);
                switch (NormalizedHash(name))
                {
                    case 926444256U when NormalizedEquals(name, "id"):
                        token = type == typeof(int) ? 0 : 1; // two tokens for right-typed and type-flexible
                        break;

                }
                tokens[i] = token;
                columnOffset++;

            }

        }
        public override global::SomeCode.InternalNesting.SomeInternalType Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
        {
            global::SomeCode.InternalNesting.SomeInternalType result = new();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case 0:
                        result.Id = reader.GetInt32(columnOffset);
                        break;
                    case 1:
                        result.Id = GetValue<int>(reader, columnOffset);
                        break;

                }
                columnOffset++;

            }
            return result;

        }

    }

    private sealed class RowFactory2 : global::Dapper.RowFactory<global::SomeCode.InternalNesting.SomeProtectedInternalType>
    {
        internal static readonly RowFactory2 Instance = new();
        private RowFactory2() {}
        public override void Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                int token = -1;
                var name = reader.GetName(columnOffset);
                var type = reader.GetFieldType(columnOffset);
                switch (NormalizedHash(name))
                {
                    case 926444256U when NormalizedEquals(name, "id"):
                        token = type == typeof(int) ? 0 : 1; // two tokens for right-typed and type-flexible
                        break;

                }
                tokens[i] = token;
                columnOffset++;

            }

        }
        public override global::SomeCode.InternalNesting.SomeProtectedInternalType Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)
        {
            global::SomeCode.InternalNesting.SomeProtectedInternalType result = new();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case 0:
                        result.Id = reader.GetInt32(columnOffset);
                        break;
                    case 1:
                        result.Id = GetValue<int>(reader, columnOffset);
                        break;

                }
                columnOffset++;

            }
            return result;

        }

    }

    private sealed class CommandFactory0 : CommonCommandFactory<global::SomeCode.InternalNesting.SomePublicType>
    {
        internal static readonly CommandFactory0 Instance = new();
        private CommandFactory0() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomePublicType args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            // if (Include(sql, commandType, "Id"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(args.Id);
                cmd.Parameters.Add(p);
            }

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomePublicType args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            // if (Include(sql, commandType, "Id"))
            {
                ps["Id"].Value = AsValue(args.Id);

            }

        }

    }

    private sealed class CommandFactory1 : CommonCommandFactory<global::SomeCode.InternalNesting.SomeInternalType>
    {
        internal static readonly CommandFactory1 Instance = new();
        private CommandFactory1() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomeInternalType args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            // if (Include(sql, commandType, "Id"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(args.Id);
                cmd.Parameters.Add(p);
            }

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomeInternalType args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            // if (Include(sql, commandType, "Id"))
            {
                ps["Id"].Value = AsValue(args.Id);

            }

        }

    }

    private sealed class CommandFactory2 : CommonCommandFactory<global::SomeCode.InternalNesting.SomeProtectedInternalType>
    {
        internal static readonly CommandFactory2 Instance = new();
        private CommandFactory2() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomeProtectedInternalType args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            // if (Include(sql, commandType, "Id"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(args.Id);
                cmd.Parameters.Add(p);
            }

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::SomeCode.InternalNesting.SomeProtectedInternalType args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            // if (Include(sql, commandType, "Id"))
            {
                ps["Id"].Value = AsValue(args.Id);

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
#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 9, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 10, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 12, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 14, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 16, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 17, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 19, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 79, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 82, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 83, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 86, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 89, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 96, 24)]
        internal static global::System.Collections.Generic.IEnumerable<dynamic> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Buffered, Text, BindResultsByName
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 22, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 25, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 28, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 31, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 34, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 36, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 40, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 43, 20)]
        internal static dynamic QueryFirst1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, SingleRow, Text, AtLeastOne, BindResultsByName
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryFirst(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 23, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 26, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 29, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 32, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 35, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 37, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 64, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 67, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 70, 24)]
        internal static dynamic QuerySingle2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 53, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 55, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 57, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 59, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 61, 20)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: int a, string b>
            // parameter map: a b
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 73, 24)]
        internal static dynamic QuerySingle4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int[] ids>
            // parameter map: ids
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 76, 24)]
        internal static dynamic QuerySingle5(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, HasParameters, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int a, string b>
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QuerySingle(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 103, 24)]
        internal static global::Foo.Customer QueryFirst6(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, SingleRow, Text, AtLeastOne, BindResultsByName
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryFirst(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 105, 24)]
        internal static int QueryFirst7(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, SingleRow, Text, AtLeastOne
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryFirst(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 123, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 133, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 146, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 147, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 149, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 152, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 153, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 155, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 168, 15)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 173, 17)]
        internal static int Execute8(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, Text
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 157, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 158, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 159, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 161, 20)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\TsqlTips.input.cs", 163, 20)]
        internal static global::Foo.Customer QuerySingle9(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingle(param, RowFactory0.Instance);

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
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    int token = -1;
                    var name = reader.GetName(columnOffset);
                    var type = reader.GetFieldType(columnOffset);
                    switch (NormalizedHash(name))
                    {
                        case 2560266987U when NormalizedEquals(name, "balance"):
                            token = type == typeof(int) ? 0 : 1; // two tokens for right-typed and type-flexible
                            break;

                    }
                    tokens[i] = token;
                    columnOffset++;

                }
                return null;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Customer result = new();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            result.Balance = reader.GetInt32(columnOffset);
                            break;
                        case 1:
                            result.Balance = GetValue<int>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int a, string b>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "a";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.a);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "b";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.b);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { a = default(int), b = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.a);
                ps[1].Value = AsValue(typed.b);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int[] ids>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "ids";
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.ids);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.ids);

            }

        }

        private sealed class CommandFactory2 : CommonCommandFactory<object>
        {
            internal static readonly CommandFactory2 Instance = new();
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
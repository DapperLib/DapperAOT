#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 10, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 14, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory1.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 18, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName, StrictTypes
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory2.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 22, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName, StrictTypes
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory3.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 26, 23)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 30, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName, StrictTypes
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory4.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 33, 23)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryStrictBind.input.cs", 37, 23)]
        internal static global::System.Threading.Tasks.Task<global::Foo.Customer> QuerySingleAsync5(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, SingleRow, Text, AtLeastOne, AtMostOne, BindResultsByName
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QuerySingleAsync(param, RowFactory4.Instance);

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
                // query columns: x, (n/a), z, (n/a), y
                global::System.Diagnostics.Debug.Assert(tokens.Length >= 5, "Query columns count mismatch");
                return null;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Customer result = new();
                int lim = global::System.Math.Min(tokens.Length, 5);
                for (int token = 0; token < lim; token++) // query-columns predefined
                {
                    switch (token)
                    {
                        case 0:
                            result.X = reader.GetInt32(columnOffset);
                            break;
                        case 2:
                            result.Z = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 4:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class RowFactory1 : global::Dapper.RowFactory<global::Foo.Customer>
        {
            internal static readonly RowFactory1 Instance = new();
            private RowFactory1() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                // query columns: x, (n/a), z, (n/a), y
                global::System.Diagnostics.Debug.Assert(tokens.Length >= 5, "Query columns count mismatch");
                return null;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Customer result = new();
                int lim = global::System.Math.Min(tokens.Length, 5);
                for (int token = 0; token < lim; token++) // query-columns predefined
                {
                    switch (token)
                    {
                        case 0:
                            result.X = reader.GetInt32(columnOffset);
                            break;
                        case 2:
                            result.Z = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 4:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class RowFactory2 : global::Dapper.RowFactory<global::Foo.Customer>
        {
            internal static readonly RowFactory2 Instance = new();
            private RowFactory2() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                // query columns: x, (n/a), z, (n/a), y
                global::System.Diagnostics.Debug.Assert(tokens.Length >= 5, "Query columns count mismatch");
                return null;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Customer result = new();
                int lim = global::System.Math.Min(tokens.Length, 5);
                for (int token = 0; token < lim; token++) // query-columns predefined
                {
                    switch (token)
                    {
                        case 0:
                            result.X = reader.GetInt32(columnOffset);
                            break;
                        case 2:
                            result.Z = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 4:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class RowFactory3 : global::Dapper.RowFactory<global::Foo.Customer>
        {
            internal static readonly RowFactory3 Instance = new();
            private RowFactory3() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
            {
                // query columns: x, (n/a), z, (n/a), y
                global::System.Diagnostics.Debug.Assert(tokens.Length >= 5, "Query columns count mismatch");
                return null;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.Customer result = new();
                int lim = global::System.Math.Min(tokens.Length, 5);
                for (int token = 0; token < lim; token++) // query-columns predefined
                {
                    switch (token)
                    {
                        case 0:
                            result.X = reader.GetInt32(columnOffset);
                            break;
                        case 2:
                            result.Z = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 4:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return result;

            }

        }

        private sealed class RowFactory4 : global::Dapper.RowFactory<global::Foo.Customer>
        {
            internal static readonly RowFactory4 Instance = new();
            private RowFactory4() {}
            public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)
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
                            result.X = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            result.X = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            result.Y = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
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
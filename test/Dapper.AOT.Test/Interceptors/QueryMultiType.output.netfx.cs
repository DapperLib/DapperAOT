#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 12, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query0(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 13, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query1(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 14, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query2(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 15, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query3(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 16, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query4(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 17, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query5(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 19, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync6(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, splitOn));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 20, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync7(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 21, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync8(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 22, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync9(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 23, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync10(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn));

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryMultiType.input.cs", 24, 30)]
        internal static global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::Foo.Customer>> QueryAsync11(this global::System.Data.IDbConnection cnn, string sql, global::System.Func<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer> map, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, string splitOn, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Async, TypedResult, Buffered, StoredProcedure, BindResultsByName, MultiMap
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.AsEnumerableAsync(
                global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBufferedAsync<global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer, global::Foo.Customer>(param, map, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, RowFactory0.Instance, splitOn));

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
                int availableColumns = reader.FieldCount - columnOffset;
                int tokenCount = global::System.Math.Min(tokens.Length, availableColumns);
                for (int i = 0; i < tokenCount; i++)
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
                // Initialize remaining tokens to -1 (unmapped)
                for (int i = tokenCount; i < tokens.Length; i++)
                {
                    tokens[i] = -1;
                }
                return tokenCount;
            }
            public override global::Foo.Customer Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                if (columnOffset > 0 && reader.IsDBNull(columnOffset)) return default!;
                global::Foo.Customer result = new();
                int tokenCount = state is int count ? count : tokens.Length;
                for (int i = 0; i < tokenCount; i++)
                {
                    var token = tokens[i];
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
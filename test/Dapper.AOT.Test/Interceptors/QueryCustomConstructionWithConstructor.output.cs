#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 10, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.ParameterlessCtor> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.ParameterlessCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 11, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.GetOnlyPropertiesViaConstructor> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.GetOnlyPropertiesViaConstructor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory1.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 12, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.RecordClass> Query2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.RecordClass
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory2.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 13, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.RecordClassSimpleCtor> Query3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.RecordClassSimpleCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory3.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 14, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.RecordStruct> Query4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.RecordStruct
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory4.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 15, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.RecordStructSimpleCtor> Query5(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.RecordStructSimpleCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory5.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 16, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.InitPropsOnly> Query6(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.InitPropsOnly
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory6.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 17, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.InitPropsAndDapperAotCtor> Query7(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.InitPropsAndDapperAotCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory7.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 18, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.OnlyNonDapperAotCtor> Query8(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.OnlyNonDapperAotCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory8.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 19, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.SingleDefaultCtor> Query9(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.SingleDefaultCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory9.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 20, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.MultipleDapperAotCtors> Query10(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.MultipleDapperAotCtors
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory10.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\QueryCustomConstructionWithConstructor.input.cs", 21, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.SingleDapperAotCtor> Query11(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, Buffered, StoredProcedure, BindResultsByName
            // returns data: global::Foo.SingleDapperAotCtor
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, RowFactory11.Instance);

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

        private sealed class RowFactory0 : global::Dapper.RowFactory<global::Foo.ParameterlessCtor>
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
            public override global::Foo.ParameterlessCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.ParameterlessCtor result = new();
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

        private sealed class RowFactory1 : global::Dapper.RowFactory<global::Foo.GetOnlyPropertiesViaConstructor>
        {
            internal static readonly RowFactory1 Instance = new();
            private RowFactory1() {}
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
            public override global::Foo.GetOnlyPropertiesViaConstructor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                int value0 = default;
                string? value1 = default;
                double? value2 = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            value0 = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            value0 = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 5:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return new global::Foo.GetOnlyPropertiesViaConstructor(value0, value1, value2);
            }
        }

        private sealed class RowFactory2 : global::Dapper.RowFactory<global::Foo.RecordClass>
        {
            internal static readonly RowFactory2 Instance = new();
            private RowFactory2() {}
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
            public override global::Foo.RecordClass Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.RecordClass result = new();
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

        private sealed class RowFactory3 : global::Dapper.RowFactory<global::Foo.RecordClassSimpleCtor>
        {
            internal static readonly RowFactory3 Instance = new();
            private RowFactory3() {}
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
            public override global::Foo.RecordClassSimpleCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                int value0 = default;
                string? value1 = default;
                double? value2 = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            value0 = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            value0 = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 5:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return new global::Foo.RecordClassSimpleCtor(value0, value1, value2);
            }
        }

        private sealed class RowFactory4 : global::Dapper.RowFactory<global::Foo.RecordStruct>
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
            public override global::Foo.RecordStruct Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.RecordStruct result = new();
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

        private sealed class RowFactory5 : global::Dapper.RowFactory<global::Foo.RecordStructSimpleCtor>
        {
            internal static readonly RowFactory5 Instance = new();
            private RowFactory5() {}
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
            public override global::Foo.RecordStructSimpleCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                int value0 = default;
                string? value1 = default;
                double? value2 = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            value0 = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            value0 = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 5:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return new global::Foo.RecordStructSimpleCtor(value0, value1, value2);
            }
        }

        private sealed class RowFactory6 : global::Dapper.RowFactory<global::Foo.InitPropsOnly>
        {
            internal static readonly RowFactory6 Instance = new();
            private RowFactory6() {}
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
            public override global::Foo.InitPropsOnly Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                int value0 = default;
                string? value1 = default;
                double? value2 = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            value0 = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            value0 = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 5:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return new global::Foo.InitPropsOnly
                {
                    X = value0,
                    Y = value1,
                    Z = value2,
                };
            }
        }

        private sealed class RowFactory7 : global::Dapper.RowFactory<global::Foo.InitPropsAndDapperAotCtor>
        {
            internal static readonly RowFactory7 Instance = new();
            private RowFactory7() {}
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
            public override global::Foo.InitPropsAndDapperAotCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                int value0 = default;
                string? value1 = default;
                double? value2 = default;
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0:
                            value0 = reader.GetInt32(columnOffset);
                            break;
                        case 3:
                            value0 = GetValue<int>(reader, columnOffset);
                            break;
                        case 1:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : reader.GetString(columnOffset);
                            break;
                        case 4:
                            value1 = reader.IsDBNull(columnOffset) ? (string?)null : GetValue<string>(reader, columnOffset);
                            break;
                        case 2:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : reader.GetDouble(columnOffset);
                            break;
                        case 5:
                            value2 = reader.IsDBNull(columnOffset) ? (double?)null : GetValue<double>(reader, columnOffset);
                            break;

                    }
                    columnOffset++;

                }
                return new global::Foo.InitPropsAndDapperAotCtor(value1)
                {
                    X = value0,
                    Z = value2,
                };
            }
        }

        private sealed class RowFactory8 : global::Dapper.RowFactory<global::Foo.OnlyNonDapperAotCtor>
        {
            internal static readonly RowFactory8 Instance = new();
            private RowFactory8() {}
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
            public override global::Foo.OnlyNonDapperAotCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.OnlyNonDapperAotCtor result = new();
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

        private sealed class RowFactory9 : global::Dapper.RowFactory<global::Foo.SingleDefaultCtor>
        {
            internal static readonly RowFactory9 Instance = new();
            private RowFactory9() {}
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
            public override global::Foo.SingleDefaultCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.SingleDefaultCtor result = new();
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

        private sealed class RowFactory10 : global::Dapper.RowFactory<global::Foo.MultipleDapperAotCtors>
        {
            internal static readonly RowFactory10 Instance = new();
            private RowFactory10() {}
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
            public override global::Foo.MultipleDapperAotCtors Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.MultipleDapperAotCtors result = new();
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

        private sealed class RowFactory11 : global::Dapper.RowFactory<global::Foo.SingleDapperAotCtor>
        {
            internal static readonly RowFactory11 Instance = new();
            private RowFactory11() {}
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
            public override global::Foo.SingleDapperAotCtor Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                global::Foo.SingleDapperAotCtor result = new();
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
#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 14, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, StoredProcedure, BindResultsByName, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int Foo>
            // parameter map: Foo
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance0).QueryBuffered(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 16, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, StoredProcedure, BindResultsByName, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int Foo, string bar>
            // parameter map: bar Foo
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance0).QueryBuffered(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 17, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, StoredProcedure, BindResultsByName, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int Foo, string bar>
            // parameter map: bar Foo
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance1).QueryBuffered(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 23, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, StoredProcedure, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int Foo>
            // parameter map: Foo
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QueryBuffered(param, RowFactory0.Instance);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 25, 24)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandProperties.input.cs", 26, 24)]
        internal static global::System.Collections.Generic.IEnumerable<global::Foo.Customer> Query4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, StoredProcedure, BindResultsByName, KnownParameters
            // takes parameter: <anonymous type: int Foo, string bar>
            // parameter map: bar Foo
            // returns data: global::Foo.Customer
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory3.Instance).QueryBuffered(param, RowFactory0.Instance);

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int Foo>
        {
            internal static readonly CommandFactory0 Instance0 = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.Foo);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.Foo);

            }
            public override bool CanPrepare => true;

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
            {
                var cmd = TryReuse(ref Storage, sql, commandType, args);
                if (cmd is null)
                {
                    cmd = base.GetCommand(connection, sql, commandType, args);
                    if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                    {
                        cmd0.FetchSize = 1024;

                    }
                }
                return cmd;
            }

            public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);
            private static global::System.Data.Common.DbCommand? Storage;

        }

        private abstract class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
        {
            // these represent different call-sites (and most likely all have different SQL etc)
            internal static readonly CommandFactory1.Cached0 Instance0 = new();
            internal static readonly CommandFactory1.Cached1 Instance1 = new();

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

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
            {
                var cmd = TryReuse(ref Storage, sql, commandType, args);
                if (cmd is null)
                {
                    cmd = base.GetCommand(connection, sql, commandType, args);
                    if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                    {
                        cmd0.FetchSize = 1024;

                    }
                }
                return cmd;
            }

            public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);
            protected abstract ref global::System.Data.Common.DbCommand? Storage {get;}

            internal sealed class Cached0 : CommandFactory1
            {
                protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;
                private static global::System.Data.Common.DbCommand? s_Storage;

            }
            internal sealed class Cached1 : CommandFactory1
            {
                protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;
                private static global::System.Data.Common.DbCommand? s_Storage;

            }

        }

        private sealed class CommandFactory2 : CommonCommandFactory<object?> // <anonymous type: int Foo>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "Foo";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.Foo);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { Foo = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.Foo);

            }
            public override bool CanPrepare => true;

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
            {
                var cmd = base.GetCommand(connection, sql, commandType, args);
                if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                {
                    cmd0.FetchSize = 1024;

                }
                return cmd;
            }
        }

        private sealed class CommandFactory3 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
        {
            internal static readonly CommandFactory3 Instance = new();
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

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
            {
                var cmd = base.GetCommand(connection, sql, commandType, args);
                if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
                {
                    cmd0.FetchSize = 1024;

                }
                return cmd;
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
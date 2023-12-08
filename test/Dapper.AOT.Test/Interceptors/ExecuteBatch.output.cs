#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 14, 24)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::System.Collections.Generic.List<global::Foo.Customer>
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.List<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 15, 24)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::System.Collections.Generic.List<global::<anonymous type: int Foo, string bar>>
            // parameter map: bar Foo
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).Execute((global::System.Collections.Generic.List<object?>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 16, 24)]
        internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.Customer[]
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::Foo.Customer[])param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 17, 24)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::System.Collections.Immutable.ImmutableArray<global::Foo.Customer>
            // parameter map: X
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).Execute((global::System.Collections.Immutable.ImmutableArray<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 18, 24)]
        internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.CustomerEnumerable
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.IEnumerable<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 19, 24)]
        internal static int Execute5(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.CustomerICollection
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.ICollection<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 20, 24)]
        internal static int Execute6(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.CustomerIList
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.IList<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 21, 24)]
        internal static int Execute7(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.CustomerIReadOnlyCollection
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.IReadOnlyCollection<global::Foo.Customer>)param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 22, 24)]
        internal static int Execute8(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, StoredProcedure, KnownParameters
            // takes parameter: global::Foo.CustomerIReadOnlyList
            // parameter map: X Y Z
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).Execute((global::System.Collections.Generic.IReadOnlyList<global::Foo.Customer>)param!);

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

        private sealed class CommandFactory0 : CommonCommandFactory<global::Foo.Customer>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.Customer args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "X";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.X);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Y";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, args.Y);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "Z";
                p.DbType = global::System.Data.DbType.Double;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.Z);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.Customer args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.X);
                ps[1].Value = AsValue(args.Y);
                ps[2].Value = AsValue(args.Z);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
        {
            internal static readonly CommandFactory1 Instance = new();
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

        }

        private sealed class CommandFactory2 : CommonCommandFactory<global::Foo.Customer>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.Customer args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "X";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.X);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.Customer args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.X);

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
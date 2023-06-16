#nullable enable
file static class DapperGeneratedInterceptors
{
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 14, 24)]
    internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::System.Collections.Generic.List<global::Foo.Customer>
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.List<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 15, 24)]
    internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::System.Collections.Generic.List<global::<anonymous type: int Foo, string bar>>
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<object?>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory1.Instance).Execute((global::System.Collections.Generic.List<object?>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 16, 24)]
    internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, StoredProcedure
        // takes parameter: global::Foo.Customer[]
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.StoredProcedure);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, global::System.Data.CommandType.StoredProcedure, commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::Foo.Customer[])param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 17, 24)]
    internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters, Text
        // takes parameter: global::System.Collections.Immutable.ImmutableArray<global::Foo.Customer>
        global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.Text);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Immutable.ImmutableArray<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 18, 24)]
    internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::Foo.CustomerEnumerable
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.IEnumerable<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 19, 24)]
    internal static int Execute5(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::Foo.CustomerICollection
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.ICollection<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 20, 24)]
    internal static int Execute6(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::Foo.CustomerIList
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.IList<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 21, 24)]
    internal static int Execute7(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::Foo.CustomerIReadOnlyCollection
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.IReadOnlyCollection<global::Foo.Customer>)param);

    }

    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\ExecuteBatch.input.cs", 22, 24)]
    internal static int Execute8(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Execute, HasParameters
        // takes parameter: global::Foo.CustomerIReadOnlyList
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);

        return global::Dapper.DapperAotExtensions.Batch<global::Foo.Customer>(cnn, transaction, sql, commandType.GetValueOrDefault(), commandTimeout ?? -1, CommandFactory0.Instance).Execute((global::System.Collections.Generic.IReadOnlyList<global::Foo.Customer>)param);

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
        private CommandFactory0() {}
        public override void AddParameters(global::System.Data.Common.DbCommand cmd, global::Foo.Customer args)
        {
            // var sql = cmd.CommandText;
            // var commandType = cmd.CommandType;
            global::System.Data.Common.DbParameter p;
            // if (Include(sql, commandType, "X"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "X";
                p.DbType = global::System.Data.DbType.Int32;
                p.Value = AsValue(args.X);
                cmd.Parameters.Add(p);
            }
            // if (Include(sql, commandType, "Y"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Y";
                p.DbType = global::System.Data.DbType.String;
                p.Value = AsValue(args.Y);
                cmd.Parameters.Add(p);
            }
            // if (Include(sql, commandType, "Z"))
            {
                p = cmd.CreateParameter();
                p.ParameterName = "Z";
                p.DbType = global::System.Data.DbType.Double;
                p.Value = AsValue(args.Z);
                cmd.Parameters.Add(p);
            }

        }
        public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, global::Foo.Customer args)
        {
            var sql = cmd.CommandText;
            var ps = cmd.Parameters;
            // if (Include(sql, commandType, "X"))
            {
                ps["X"].Value = AsValue(args.X);

            }
            // if (Include(sql, commandType, "Y"))
            {
                ps["Y"].Value = AsValue(args.Y);

            }
            // if (Include(sql, commandType, "Z"))
            {
                ps["Z"].Value = AsValue(args.Z);

            }

        }

    }

    private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int Foo, string bar>
    {
        internal static readonly CommandFactory1 Instance = new();
        private CommandFactory1() {}
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
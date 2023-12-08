#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CacheCommand.input.cs", 15, 24)]
        internal static int Execute0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int id, string bar>
            // parameter map: bar id
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance0).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CacheCommand.input.cs", 16, 24)]
        internal static int Execute1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int id, string bar>
            // parameter map: bar id
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance1).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CacheCommand.input.cs", 19, 24)]
        internal static int Execute2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, CacheCommand, KnownParameters
            // takes parameter: <anonymous type: int id, string bar>
            // parameter map: id
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance0).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CacheCommand.input.cs", 22, 24)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: int id, string bar>
            // parameter map: (deferred)
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).Execute(param);

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

        private abstract class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int id, string bar>
        {
            // these represent different call-sites (and most likely all have different SQL etc)
            internal static readonly CommandFactory0.Cached0 Instance0 = new();
            internal static readonly CommandFactory0.Cached1 Instance1 = new();

            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.id);
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
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.id);
                ps[1].Value = AsValue(typed.bar);

            }
            public override bool CanPrepare => true;

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
                 => TryReuse(ref Storage, sql, commandType, args) ?? base.GetCommand(connection, sql, commandType, args);

            public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);
            protected abstract ref global::System.Data.Common.DbCommand? Storage {get;}

            internal sealed class Cached0 : CommandFactory0
            {
                protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;
                private static global::System.Data.Common.DbCommand? s_Storage;

            }
            internal sealed class Cached1 : CommandFactory0
            {
                protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;
                private static global::System.Data.Common.DbCommand? s_Storage;

            }

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: int id, string bar>
        {
            internal static readonly CommandFactory1 Instance0 = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.id);

            }
            public override bool CanPrepare => true;

            public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,
                string sql, global::System.Data.CommandType commandType, object? args)
                 => TryReuse(ref Storage, sql, commandType, args) ?? base.GetCommand(connection, sql, commandType, args);

            public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);
            private static global::System.Data.Common.DbCommand? Storage;

        }

        private sealed class CommandFactory2 : CommonCommandFactory<object?> // <anonymous type: int id, string bar>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                var sql = cmd.CommandText;
                var commandType = cmd.CommandType;
                if (Include(sql, commandType, "id"))
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "id";
                    p.DbType = global::System.Data.DbType.Int32;
                    p.Direction = global::System.Data.ParameterDirection.Input;
                    p.Value = AsValue(typed.id);
                    ps.Add(p);

                }

                if (Include(sql, commandType, "bar"))
                {
                    p = cmd.CreateParameter();
                    p.ParameterName = "bar";
                    p.DbType = global::System.Data.DbType.String;
                    p.Direction = global::System.Data.ParameterDirection.Input;
                    SetValueWithDefaultSize(p, typed.bar);
                    ps.Add(p);

                }

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { id = default(int), bar = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                var sql = cmd.CommandText;
                var commandType = cmd.CommandType;
                if (Include(sql, commandType, "id"))
                {
                    ps["id"].Value = AsValue(typed.id);

                }
                if (Include(sql, commandType, "bar"))
                {
                    ps["bar"].Value = AsValue(typed.bar);

                }

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
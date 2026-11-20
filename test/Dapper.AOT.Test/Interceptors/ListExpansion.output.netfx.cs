#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 14, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: int[] ids>
            // parameter map: ids
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory0.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 15, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query1(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: List<int> ids, string region>
            // parameter map: ids region
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory1.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 16, 24)]
        internal static global::System.Collections.Generic.IEnumerable<int> Query2(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: string[] names>
            // parameter map: names
            // returns data: int
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory2.Instance).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Value<int>());

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 19, 24)]
        internal static int Execute3(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: <anonymous type: byte[] photo, int id>
            // parameter map: id photo
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory3.Instance).Execute(param);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 20, 24)]
        internal static int Execute4(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::Foo.WithPhoto[]
            // parameter map: id photo
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory4.Instance).Execute((global::Foo.WithPhoto[])param!);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors/ListExpansion.input.cs", 21, 24)]
        internal static int Execute5(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Execute, HasParameters, Text, KnownParameters
            // takes parameter: global::Foo.WithHash
            // parameter map: hash id
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(param is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), CommandFactory5.Instance).Execute((global::Foo.WithHash)param!);

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int[] ids>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "ids", typed.ids);
                #pragma warning restore CS0618

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(int[])! }); // expected shape
                var ps = cmd.Parameters;

            }

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: List<int> ids, string region>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(global::System.Collections.Generic.List<int>)!, region = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "ids", typed.ids);
                #pragma warning restore CS0618

                p = cmd.CreateParameter();
                p.ParameterName = "region";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.region);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { ids = default(global::System.Collections.Generic.List<int>)!, region = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[1].Value = AsValue(typed.region);

            }

        }

        private sealed class CommandFactory2 : CommonCommandFactory<object?> // <anonymous type: string[] names>
        {
            internal static readonly CommandFactory2 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { names = default(string[])! }); // expected shape
                var ps = cmd.Parameters;
                #pragma warning disable CS0618 // list-expansion: this *is* the library usage
                global::Dapper.SqlMapper.PackListParameters(cmd.Command!, "names", typed.names);
                #pragma warning restore CS0618

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { names = default(string[])! }); // expected shape
                var ps = cmd.Parameters;

            }

        }

        private sealed class CommandFactory3 : CommonCommandFactory<object?> // <anonymous type: byte[] photo, int id>
        {
            internal static readonly CommandFactory3 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { photo = default(byte[])!, id = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "photo";
                p.DbType = global::System.Data.DbType.Binary;
                p.Size = -1;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.photo);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { photo = default(byte[])!, id = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.photo);
                ps[1].Value = AsValue(typed.id);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory4 : CommonCommandFactory<global::Foo.WithPhoto>
        {
            internal static readonly CommandFactory4 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.WithPhoto args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "photo";
                p.DbType = global::System.Data.DbType.Binary;
                p.Size = -1;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.photo);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.id);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.WithPhoto args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.photo);
                ps[1].Value = AsValue(args.id);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory5 : CommonCommandFactory<global::Foo.WithHash>
        {
            internal static readonly CommandFactory5 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.WithHash args)
            {
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "id";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(args.id);
                ps.Add(p);

                p = cmd.CreateParameter();
                p.ParameterName = "hash";
                p.DbType = global::System.Data.DbType.Binary;
                p.Size = 32;
                p.Direction = global::System.Data.ParameterDirection.Output;
                p.Value = global::System.DBNull.Value;
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, global::Foo.WithHash args)
            {
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(args.id);
                ps[1].Value = global::System.DBNull.Value;

            }
            public override bool RequirePostProcess => true;

            public override void PostProcess(in global::Dapper.UnifiedCommand cmd, global::Foo.WithHash args, int rowCount)
            {
                var ps = cmd.Parameters;
                args.hash = Parse<byte[]>(ps[1].Value);
                base.PostProcess(in cmd, args, rowCount);

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
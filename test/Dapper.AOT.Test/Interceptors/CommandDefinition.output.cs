#nullable enable
#pragma warning disable IDE0078 // unnecessary suppression is necessary
#pragma warning disable CS9270 // SDK-dependent change to interceptors usage
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 16, 23)]
        internal static string? ExecuteScalar0(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Execute, TypedResult, HasParameters, StoredProcedure, Scalar, KnownParameters
            // takes parameter: <anonymous type: int X>
            // parameter map: X
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert((command.CommandType ?? global::Dapper.DapperAotExtensions.GetCommandType(command.CommandText)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(command.Buffered is false); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, CommandFactory0.Instance).ExecuteScalar<string>(command.Parameters);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 24, 24)]
        internal static global::System.Threading.Tasks.Task<string?> ExecuteScalarAsync1(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Execute, Async, TypedResult, HasParameters, StoredProcedure, Scalar, KnownParameters
            // takes parameter: <anonymous type: int X>
            // parameter map: X
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert((command.CommandType ?? global::Dapper.DapperAotExtensions.GetCommandType(command.CommandText)) == global::System.Data.CommandType.StoredProcedure);
            global::System.Diagnostics.Debug.Assert(command.Buffered is false); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, CommandFactory0.Instance).ExecuteScalarAsync<string>(command.Parameters, cancellationToken: command.CancellationToken);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 32, 24)]
        internal static global::System.Threading.Tasks.Task<string?> ExecuteScalarAsync2(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Execute, Async, TypedResult, HasParameters, Text, Scalar, KnownParameters
            // takes parameter: <anonymous type: int X>
            // parameter map: X
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert((command.CommandType ?? global::Dapper.DapperAotExtensions.GetCommandType(command.CommandText)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(command.Buffered is false); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, CommandFactory0.Instance).ExecuteScalarAsync<string>(command.Parameters, cancellationToken: command.CancellationToken);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 38, 24)]
        internal static string? ExecuteScalar3(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Execute, TypedResult, HasParameters, Text, Scalar, KnownParameters
            // takes parameter: <anonymous type: int X>
            // parameter map: X
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert((command.CommandType ?? global::Dapper.DapperAotExtensions.GetCommandType(command.CommandText)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(command.Buffered is false); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, CommandFactory0.Instance).ExecuteScalar<string>(command.Parameters);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 48, 24)]
        internal static global::System.Threading.Tasks.Task<string?> ExecuteScalarAsync4(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Execute, Async, TypedResult, Scalar
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert(command.Buffered is false); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, DefaultCommandFactory).ExecuteScalarAsync<string>(command.Parameters, cancellationToken: command.CancellationToken);

        }

        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\CommandDefinition.input.cs", 51, 34)]
        internal static global::System.Collections.Generic.IEnumerable<string> Query5(this global::System.Data.IDbConnection cnn, global::Dapper.CommandDefinition command)
        {
            // Query, TypedResult, HasParameters, Buffered, Text, KnownParameters
            // takes parameter: <anonymous type: string name>
            // parameter map: name
            // returns data: string
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(command.CommandText));
            global::System.Diagnostics.Debug.Assert((command.CommandType ?? global::Dapper.DapperAotExtensions.GetCommandType(command.CommandText)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(command.Buffered is true); 
            global::System.Diagnostics.Debug.Assert(command.Parameters is not null);

            return global::Dapper.DapperAotExtensions.Command(cnn, command.Transaction, command.CommandText, command.CommandType ?? default, command.CommandTimeout ?? default, CommandFactory1.Instance).QueryBuffered(command.Parameters, global::Dapper.RowFactory.Inbuilt.Value<string>());

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

        private sealed class CommandFactory0 : CommonCommandFactory<object?> // <anonymous type: int X>
        {
            internal static readonly CommandFactory0 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { X = default(int) }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "X";
                p.DbType = global::System.Data.DbType.Int32;
                p.Direction = global::System.Data.ParameterDirection.Input;
                p.Value = AsValue(typed.X);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { X = default(int) }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.X);

            }
            public override bool CanPrepare => true;

        }

        private sealed class CommandFactory1 : CommonCommandFactory<object?> // <anonymous type: string name>
        {
            internal static readonly CommandFactory1 Instance = new();
            public override void AddParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { name = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                global::System.Data.Common.DbParameter p;
                p = cmd.CreateParameter();
                p.ParameterName = "name";
                p.DbType = global::System.Data.DbType.String;
                p.Direction = global::System.Data.ParameterDirection.Input;
                SetValueWithDefaultSize(p, typed.name);
                ps.Add(p);

            }
            public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, object? args)
            {
                var typed = Cast(args, static () => new { name = default(string)! }); // expected shape
                var ps = cmd.Parameters;
                ps[0].Value = AsValue(typed.name);

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
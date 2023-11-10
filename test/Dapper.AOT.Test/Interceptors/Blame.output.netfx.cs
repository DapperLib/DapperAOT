#nullable enable
namespace Dapper.AOT // interceptors must be in a known namespace
{
    file static class DapperGeneratedInterceptors
    {
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 12, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 13, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 14, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 15, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 16, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 17, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 18, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 19, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 22, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 23, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 28, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 33, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 36, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 38, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 40, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 42, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 44, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 46, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 50, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 52, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 58, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 64, 16)]
        [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Blame.input.cs", 66, 16)]
        internal static global::System.Collections.Generic.IEnumerable<dynamic> Query0(this global::System.Data.IDbConnection cnn, string sql, object? param, global::System.Data.IDbTransaction? transaction, bool buffered, int? commandTimeout, global::System.Data.CommandType? commandType)
        {
            // Query, Buffered, Text, BindResultsByName
            global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));
            global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.Text);
            global::System.Diagnostics.Debug.Assert(buffered is true);
            global::System.Diagnostics.Debug.Assert(param is null);

            return global::Dapper.DapperAotExtensions.Command(cnn, transaction, sql, global::System.Data.CommandType.Text, commandTimeout.GetValueOrDefault(), DefaultCommandFactory).QueryBuffered(param, global::Dapper.RowFactory.Inbuilt.Dynamic);

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
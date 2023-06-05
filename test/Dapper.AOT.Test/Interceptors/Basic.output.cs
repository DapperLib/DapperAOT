
file static class DapperGeneratedInterceptors
{
#pragma warning disable CS0618
    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("Interceptors\\Basic.input.cs", 27, 13)]
    internal static unsafe global::Foo.Customer QueryFirst0(this global::System.Data.IDbConnection cnn, string sql, object param, global::System.Data.IDbTransaction transaction, int? commandTimeout, global::System.Data.CommandType? commandType)
    {
        // Query, TypedResult, HasParameters, Buffered, First
        // takes parameter: <anonymous type: int Foo, string bar>
        // returns data: global::Foo.Customer
        global::System.Diagnostics.Debug.Assert(commandType is null);
        global::System.Diagnostics.Debug.Assert(param is not null);
        return global::Dapper.Internal.InterceptorHelpers.UnsafeQueryFirst(
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(cnn), sql,
            param,
            global::Dapper.Internal.InterceptorHelpers.TypeCheck(transaction),
            commandTimeout, &CommandBuilder, &ColumnTokenizer0, &RowReader0);

        static void CommandBuilder(global::System.Data.Common.DbCommand cmd, object args)
        {
            InitCommand(cmd);
            cmd.CommandType = global::System.Data.CommandType.Text;

        }
    }

    private static void ColumnTokenizer0(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int fieldOffset)
    {
        // tokenize global::Foo.Customer

    }
    private static global::Foo.Customer RowReader0(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int fieldOffset)
    {
        // parse global::Foo.Customer
        throw new global::System.NotImplementedException();
    }
    private static void InitCommand(global::System.Data.Common.DbCommand cmd)
    {
        // apply special per-provider command initialization logic
        if (cmd is global::Oracle.ManagedDataAccess.Client.OracleCommand cmd0)
        {
            cmd0.BindByName = true;
            cmd0.InitialLONGFetchSize = -1;

        }
    }

#pragma warning restore CS0618
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

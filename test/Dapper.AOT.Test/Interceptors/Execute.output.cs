
#nullable enable
file static partial class DapperGeneratedInterceptors
{
#pragma warning disable CS0618

    // placeholder for per-provider setup rules
    static partial void InitCommand(global::System.Data.Common.DbCommand cmd);

    static partial void InitCommand(global::System.Data.Common.DbCommand cmd)
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

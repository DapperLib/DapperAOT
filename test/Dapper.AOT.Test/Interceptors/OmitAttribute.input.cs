using Dapper;
using System.Data.SqlClient;

[module: DapperAot]
public static class Foo
{
    static void SystemData(SqlConnection db)
    {
        db.Execute("print GETDATE();");
    }
}


namespace System.Runtime.CompilerServices
{
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed internal class InterceptsLocationAttribute : global::System.Attribute
    {
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }
    }
}
namespace InterceptionExecutables
{
    using System;
    using System.IO;
    using Dapper;
    using System.Threading.Tasks;
    using InterceptionExecutables.IncludedTypes;
    
    [DapperAot] // Enabling Dapper AOT!
    public static class Program
    {
        public static async Task<object> ExecuteAsync(IDbConnection dbConnection)
        {
            <your user code goes here>
        }
    }
                 
    <additional code goes here>
}
namespace InterceptionExecutables
{
    using System;
    using System.IO;
    using Dapper;
    using System.Threading.Tasks;
    
    // this is just a sample for easy test-writing
    public static class Program
    {
        public static async Task<object> ExecuteAsync(IDbConnection dbConnection)
        {
            <your user code goes here>
        }
    }
                 
    <additional code goes here>
}
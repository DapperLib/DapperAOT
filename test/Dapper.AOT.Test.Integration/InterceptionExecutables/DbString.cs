using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace InterceptionExecutables
{
    using System;
    using System.IO;
    using Dapper;
    using System.Threading.Tasks;
                 
    public static class Program
    {
        public static async Task<int> ExecuteAsync(IDbConnection dbConnection)
        {
            var res = await dbConnection.QueryAsync<int>("SELECT count(*) FROM dbStringTable");
            return res.First();
        }
    }
}
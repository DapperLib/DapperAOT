using System.Data;
using System.Data.SqlClient;
using System.Linq;

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
        public static Poco Execute(IDbConnection dbConnection)
            => ExecuteAsync(dbConnection).GetAwaiter().GetResult();
        
        public static async Task<Poco> ExecuteAsync(IDbConnection dbConnection)
        {
            var a = GetValue();
            return new Poco() { Id = 1, Name = a };
            
            // var results = await dbConnection.QueryAsync<Poco>("select * from dbStringTestsTable where id = @Id and Name = @Name", new
            // {
            //     Name = new DbString
            //     {
            //         Value = "me testing!",
            //         IsFixedLength = false,
            //         Length = 11
            //     },
            //     
            //     Id = 1,
            // });
            //
            // return results.First();
        }

        public static string GetValue() => "my-data";
    }
}
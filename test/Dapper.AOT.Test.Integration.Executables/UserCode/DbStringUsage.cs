using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

[DapperAot]
public class DbStringUsage : IExecutable<DbStringPoco>
{
    public DbStringPoco Execute(IDbConnection connection)
    {
        var a = GetSomething();
        return new DbStringPoco()
        {
            Id = 1,
            Name = a
        };
        
        // var results = connection.Query<DbStringPoco>($"select * from {DbStringPoco.TableName}");
        // return results.First();
    }

    static string GetSomething()
    {
        return "qwe";
    }
}
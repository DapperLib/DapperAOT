using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

[DapperAot]
public class DbStringUsage : IExecutable<DbStringPoco>
{
    public DbStringPoco Execute(IDbConnection connection)
    {
        var results = connection.Query<DbStringPoco>(
            $"select * from {DbStringPoco.TableName} where name = @name",
            new
            {
                name = new DbString
                {
                    Value = "my-dbString"
                }
            });
        return results.First();
    }
}
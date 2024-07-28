using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

public class DbStringUsage : IExecutable<DbStringPoco>
{
    public DbStringPoco Execute(IDbConnection connection)
    {
        var results = connection.Query<DbStringPoco>($"select * from {DbStringPoco.TableName}");
        return results.First();
    }
}
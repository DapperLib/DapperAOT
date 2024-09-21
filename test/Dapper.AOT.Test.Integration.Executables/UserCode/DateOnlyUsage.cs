using System;
using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

[DapperAot]
public class DateOnlyUsage : IExecutable<DateOnlyTimeOnlyPoco>
{
    public DateOnlyTimeOnlyPoco Execute(IDbConnection connection)
    {
        var results = connection.Query<DateOnlyTimeOnlyPoco>($"select * from {DateOnlyTimeOnlyPoco.TableName}");
        return results.First();
    }
}
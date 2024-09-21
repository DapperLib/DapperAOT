using System;
using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

[DapperAot]
public class DateOnlyUsageWithDateFilter : IExecutable<DateOnlyTimeOnlyPoco>
{
    public DateOnlyTimeOnlyPoco Execute(IDbConnection connection)
    {
        var results = connection.Query<DateOnlyTimeOnlyPoco>(
            $"""
                select * from {DateOnlyTimeOnlyPoco.TableName}
                where date = @date
            """,
            new { date = DateOnlyTimeOnlyPoco.SpecificDate }
        );

        return results.First();
    }
}
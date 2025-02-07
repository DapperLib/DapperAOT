using System.Data;
using System.Linq;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode.DateOnlyTimeOnly;

[DapperAot]
public class DateOnlyInsert : IExecutable<DateOnlyTimeOnlyPoco>
{
    public DateOnlyTimeOnlyPoco Execute(IDbConnection connection)
    {
        connection.Execute(
            $"""
                insert into {DateOnlyTimeOnlyPoco.TableName}(id, date, time)
                values (@id, @date, @time)
                on conflict (id) do nothing;
            """,
            new DateOnlyTimeOnlyPoco { Id = 2, Date = DateOnlyTimeOnlyPoco.SpecificDate, Time = DateOnlyTimeOnlyPoco.SpecificTime }
        );

        var results = connection.Query<DateOnlyTimeOnlyPoco>(
            $"""
                select * from {DateOnlyTimeOnlyPoco.TableName}
                where id = 2
            """
        );

        return results.First();
    }
}
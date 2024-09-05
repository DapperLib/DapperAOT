using System;
using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode;
using Dapper.AOT.Test.Integration.Setup;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class DateOnlyTimeOnlyTests : IntegrationTestsBase
{
    public DateOnlyTimeOnlyTests(PostgresqlFixture fixture) : base(fixture)
    {
    }

    protected override void SetupDatabase(IDbConnection dbConnection)
    {
        base.SetupDatabase(dbConnection);

        var date = $"{DateOnlyTimeOnlyPoco.SpecificDate.Year}-{DateOnlyTimeOnlyPoco.SpecificDate.Month:2}-{DateOnlyTimeOnlyPoco.SpecificDate.Day:2}";
        
        dbConnection.Execute($"""
            CREATE TABLE IF NOT EXISTS {DateOnlyTimeOnlyPoco.TableName}(
              id     integer PRIMARY KEY,
              dateOnly DATE,
              timeOnly TIME
            );

            TRUNCATE {DateOnlyTimeOnlyPoco.TableName};

            INSERT INTO {DateOnlyTimeOnlyPoco.TableName} (id, dateOnly, timeOnly)
            VALUES (1, '{date}', current_timestamp)
        """);
    }

    [Fact]
    public void DateOnly_BasicUsage_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DateOnlyUsage, DateOnlyTimeOnlyPoco>(DbConnection);
        Assert.True(result.Id.Equals(1));
    }
}
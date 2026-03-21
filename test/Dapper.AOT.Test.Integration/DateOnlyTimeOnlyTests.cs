using System;
using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode.DateOnlyTimeOnly;
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

        dbConnection.Execute($"""
            CREATE TABLE IF NOT EXISTS {DateOnlyTimeOnlyPoco.TableName}(
              id     integer PRIMARY KEY,
              date DATE,
              time TIME
            );

            TRUNCATE {DateOnlyTimeOnlyPoco.TableName};

            INSERT INTO {DateOnlyTimeOnlyPoco.TableName} (id, date, time)
            VALUES (1, '{DateOnlyTimeOnlyPoco.SpecificDate.ToString("yyyy-MM-dd")}', '{DateOnlyTimeOnlyPoco.SpecificTime.ToString("HH:mm:ss")}')
        """);
    }

    [Fact]
    public void DateOnly_BasicUsage_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DateOnlyTimeOnlyUsage, DateOnlyTimeOnlyPoco>(DbConnection);
        Assert.Equal(1, result.Id);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificDate, result.Date);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificTime, result.Time);
    }

    [Fact]
    public void DateOnly_WithDateFilter_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DateOnlyUsageWithDateFilter, DateOnlyTimeOnlyPoco>(DbConnection);
        Assert.Equal(1, result.Id);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificDate, result.Date);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificTime, result.Time);
    }

    [Fact]
    public void DateOnly_WithTimeFilter_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DateOnlyUsageWithTimeFilter, DateOnlyTimeOnlyPoco>(DbConnection);
        Assert.Equal(1, result.Id);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificDate, result.Date);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificTime, result.Time);
    }

    [Fact]
    public void DateOnly_Inserts_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DateOnlyInsert, DateOnlyTimeOnlyPoco>(DbConnection);
        Assert.Equal(2, result.Id);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificDate, result.Date);
        Assert.Equal(DateOnlyTimeOnlyPoco.SpecificTime, result.Time);
    }
}
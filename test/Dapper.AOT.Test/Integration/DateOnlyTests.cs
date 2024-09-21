using System;
using Dapper.Internal;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class DateOnlyTests
{
    static DateTime TimeStampConst = new DateTime(year: 2020, month: 2, day: 2, hour: 2, minute: 2, second: 2, kind: DateTimeKind.Utc);

#if NET6_0_OR_GREATER
    static DateOnly DateOnlyConst = new DateOnly(year: 2021, month: 1, day: 1);
    static TimeOnly TimeOnlyConst = new TimeOnly(hour: 3, minute: 3, second: 3);
#endif

    private PostgresqlFixture _fixture;

    public DateOnlyTests(PostgresqlFixture fixture)
    {
        _fixture = fixture;
        fixture.NpgsqlConnection.Execute($"""
            CREATE TABLE IF NOT EXISTS date_only_table(
                 type_timestamp timestamp,
                 type_date date,
                 type_time time
            );
            TRUNCATE date_only_table;

            INSERT INTO date_only_table (type_timestamp, type_date, type_time)
            VALUES ('{TimeStampConst.ToString("yyyy-MM-dd HH:mm:ss")}', '2021-01-01', '03:03:03');
         """);
    }   
    
    [Fact]
    public void ReadTimestamp()
    {
        using var cmd = _fixture.NpgsqlConnection.CreateCommand();
        cmd.CommandText = "select type_timestamp from date_only_table";
        using var reader = cmd.ExecuteReader();

        var readResult = reader.Read();
        Assert.True(readResult);

        // DbReader will return `DateTime` here
        var value = reader.GetValue(0);

        var dateTime = CommandUtils.As<DateTime>(value);
        Assert.Equal(TimeStampConst, dateTime);

#if NET6_0_OR_GREATER
        // DateTime has Date and Time, so it can be cast to DateOnly and TimeOnly
        var dateOnly = CommandUtils.As<DateOnly>(value);
        Assert.Equal(DateOnly.FromDateTime(TimeStampConst), dateOnly);

        var timeOnly = CommandUtils.As<TimeOnly>(value);
        Assert.Equal(TimeOnly.FromDateTime(TimeStampConst), timeOnly);
#endif
    }

    [Fact]
    public void ReadDateOnly()
    {
        using var cmd = _fixture.NpgsqlConnection.CreateCommand();
        cmd.CommandText = "select type_date from date_only_table";
        using var reader = cmd.ExecuteReader();

        var readResult = reader.Read();
        Assert.True(readResult);

        // DbReader will return `DateTime` here
        var value = reader.GetValue(0);

        // technically, date can be interpreted as DateTime.
        // so lets ensure it is possible to cast to DateTime with default time, but existing year-month-day
        var dateTime = CommandUtils.As<DateTime>(value);
        Assert.Equal(new DateTime(year: 2021, month: 1, day: 1), dateTime);

#if NET6_0_OR_GREATER
        var dateOnly = CommandUtils.As<DateOnly>(value);
        Assert.Equal(DateOnlyConst, dateOnly);

        // it is possible to cast, but it returns default timeOnly, because there is no "time" in actual data
        // not sure if we need to explicitly throw here to show it's not actual time
        var timeOnly = CommandUtils.As<TimeOnly>(value);
        Assert.Equal(default(TimeOnly), timeOnly);
#endif
    }

    [Fact]
    public void ReadTimeOnly()
    {
        using var cmd = _fixture.NpgsqlConnection.CreateCommand();
        cmd.CommandText = "select type_time from date_only_table";
        using var reader = cmd.ExecuteReader();

        var readResult = reader.Read();
        Assert.True(readResult);

        // DbReader will return `TimeSpan` here
        var value = reader.GetValue(0);

        // TimeSpan is not a DateTime, so lets ensure it throws on casting to DateTime
        Assert.Throws<InvalidCastException>(() => CommandUtils.As<DateTime>(value));

#if NET6_0_OR_GREATER
        // we cant cast TimeSpan to DateOnly - therefore it's expected to throw
        Assert.Throws<InvalidCastException>(() => CommandUtils.As<DateOnly>(value));

        var timeOnly = CommandUtils.As<TimeOnly>(value);
        Assert.Equal(TimeOnlyConst, timeOnly);
#endif
    }
}
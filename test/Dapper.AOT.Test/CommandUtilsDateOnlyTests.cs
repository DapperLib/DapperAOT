#if NET6_0_OR_GREATER
using System;
using Dapper.Internal;
using Xunit;

namespace Dapper.AOT.Test
{
    // Npgsql 10 hands back DateOnly (not DateTime) for "date" columns; these are the
    // docker-free half of DateOnlyTimeOnlyPostgreSqlTests, so the coverage does not
    // depend on a live container (see #202)
    public class CommandUtilsDateOnlyTests
    {
        private static readonly object BoxedDateOnly = new DateOnly(2021, 1, 2);

        [Fact]
        public void As_DateOnlyToDateTime()
            => Assert.Equal(new DateTime(2021, 1, 2), CommandUtils.As<DateTime>(BoxedDateOnly));

        [Fact]
        public void As_DateOnlyToNullableDateTime()
            => Assert.Equal(new DateTime(2021, 1, 2), CommandUtils.As<DateTime?>(BoxedDateOnly));

        [Fact] // a date has no time component; same answer a zero-time DateTime gives
        public void As_DateOnlyToTimeOnly()
            => Assert.Equal(default, CommandUtils.As<TimeOnly>(BoxedDateOnly));

        [Fact]
        public void As_DateOnlyToNullableTimeOnly()
            => Assert.Equal(default(TimeOnly), CommandUtils.As<TimeOnly?>(BoxedDateOnly));

        [Fact] // pass-through, unchanged
        public void As_DateOnlyToDateOnly()
            => Assert.Equal(new DateOnly(2021, 1, 2), CommandUtils.As<DateOnly>(BoxedDateOnly));

        [Fact] // the pre-Npgsql-10 shape, unchanged
        public void As_DateTimeToDateOnly()
            => Assert.Equal(new DateOnly(2021, 1, 2), CommandUtils.As<DateOnly>((object)new DateTime(2021, 1, 2)));
    }
}
#endif

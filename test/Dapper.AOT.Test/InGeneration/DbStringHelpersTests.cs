using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Xunit;

namespace Dapper.AOT.Test.InGeneration
{

    public class DbStringHelpersTests
    {
        [Theory]
        [InlineData(false, false, "qweqwe", DbType.String, 60)]
        [InlineData(false, true, "qweqwe", DbType.StringFixedLength, 60)]
        [InlineData(true, false, "qweqwe", DbType.AnsiString, 60)]
        [InlineData(true, true, "qweqwe", DbType.AnsiStringFixedLength, 60)]
        public void ConfigureDbString_ShouldProperlySetupDbParameter(bool isAnsi, bool isFixedLength, string dbStringValue, DbType expectedDbType, int expectedSize)
        {
            var param = CreateDbParameter();
            var dbString = new DbString
            {
                IsAnsi = isAnsi,
                IsFixedLength = isFixedLength,
                Value = dbStringValue,
                Length = 60
            };

            Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(param, dbString);

            Assert.Equal(expectedSize, param.Size);
            Assert.Equal(expectedDbType, param.DbType);
        }

        DbParameter CreateDbParameter() => new SqlParameter();
    }
}

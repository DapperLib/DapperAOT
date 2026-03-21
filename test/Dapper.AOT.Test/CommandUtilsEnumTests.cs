using Dapper.AOT.Test.Integration;
using Dapper.Internal;
using Xunit;

namespace Dapper.AOT.Test
{
    public class CommandUtilsEnumTests
    {
        [Theory]
        [InlineData(1L, SomeEnum.A)]  // Int64 (SQLite)
        [InlineData(2, SomeEnum.B)]   // Int32 (SQL Server)
        public void As_NumericToEnum(object value, SomeEnum expected)
        {
            Assert.Equal(expected, CommandUtils.As<SomeEnum>(value));
        }

        [Fact]
        public void As_StringToEnum()
        {
            Assert.Equal(SomeEnum.B, CommandUtils.As<SomeEnum>("B"));
        }
    }
}

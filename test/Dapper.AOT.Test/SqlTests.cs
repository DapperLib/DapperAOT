using Dapper.Internal;
using Xunit;

namespace Dapper.AOT.Test;

public class SqlTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("foo", "")]
    [InlineData("foo @bar", "bar")]
    [InlineData("foo @bar,@blap, :bloop", "bar blap bloop")]
    [InlineData("foo (@bar)", "bar")]
    [InlineData("""
        select *
        from SomeTable
        where x = @x
        and y = @y+@z
        """, "x y z")]
    public void DetectParameters(string sql, string expected)
    {
        Assert.Equal(expected, string.Join(' ', SqlTools.GetParameters(sql)));
    }
}

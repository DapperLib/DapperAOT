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
    [InlineData("""
       do thing;
       """, "")]
    public void DetectParameters(string sql, string expected)
    {
        Assert.Equal(expected, string.Join(" ", SqlTools.GetParameters(sql)));
    }


    [Theory]
    [InlineData("", false)]
    [InlineData("*", true)]
    [InlineData("?", true, true)]
    [InlineData("bar", false)]
    [InlineData("bar blap", false)]
    [InlineData("name", true)]
    [InlineData("name bar", true)]
    [InlineData("bar name", true)]
    [InlineData("aname bar", false)]
    [InlineData("namea bar", false)]
    [InlineData("anamea bar", false)]
    [InlineData("bar namea", false)]
    [InlineData("bar aname", false)]
    [InlineData("bar anamea", false)]
    public void IncludeParameter(string map, bool expected, bool testExpected = false)
    {
        Assert.Equal(expected, SqlTools.IncludeParameter(map, "name", out var testActual));
        Assert.Equal(testExpected, testActual);
    }
}

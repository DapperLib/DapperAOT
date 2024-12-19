#if NET6_0_OR_GREATER

using System.Drawing;
using Xunit;
using static Dapper.Command;

namespace Dapper.AOT.Test;

public class SqlBuilderTests
{
    [Fact]
    public void BasicSqlInterpolated()
    {
        string region = "North";
        int id = 42;
        SqlBuilder sql = $"""
            select top 5 *
            from Customers
            where Id=@{id} and Region=@{region}
            """;

        Assert.Equal(2, sql.Parameters.Length);

        var finalSql = sql.GetSql();
        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@id and Region=@region
            """, finalSql);

        var p = sql.Parameters[0];
        Assert.Equal("@id", p.Name);
        Assert.Equal(42, p.Value);
        Assert.Equal(0, p.Size);

        p = sql.Parameters[1];
        Assert.Equal("@region", p.Name);
        Assert.Equal("North", p.Value);
        Assert.Equal(0, p.Size);

        var sqlCopy = sql.GetSql();
#if NET9_0_OR_GREATER
        Assert.Same(finalSql, sqlCopy);
#else
        Assert.NotSame(finalSql, sqlCopy);
#endif

        sql.Clear();
        Assert.True(sql.Parameters.IsEmpty);
        Assert.Equal("", sql.GetSql());

    }
}


#endif

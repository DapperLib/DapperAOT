#if NET6_0_OR_GREATER

using System.Data.Common;
using System.Data.SqlClient;
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
        Assert.False(sql.HasInjectedValue);

        var finalSql = sql.GetSql();
        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@id and Region=@region
            """, finalSql);

        Assert.Equal([
            new Parameter("@id", 42),
            new Parameter("@region", "North")
            ], sql.Parameters.ToArray());

        var sqlCopy = sql.GetSql();
#if NET9_0_OR_GREATER
        Assert.Same(finalSql, sqlCopy); // reused when possible
#else
        Assert.NotSame(finalSql, sqlCopy);
#endif

        sql.Clear();
        Assert.True(sql.Parameters.IsEmpty);
        Assert.Equal("", sql.GetSql());
    }

    [Fact]
    public void BasicSqlInterpolatedWithExpression()
    {
        string region = "North";
        int id = 42, area = 1;
        SqlBuilder sql = $"""
            select top 5 *
            from Customers
            where Id=@{id + 1} and Region=@{region} and Area=@{area}
            """;

        Assert.Equal(3, sql.Parameters.Length);
        Assert.False(sql.HasInjectedValue);

        var finalSql = sql.GetSql();
        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@p0 and Region=@region and Area=@area
            """, finalSql);

        Assert.Equal([
            new Parameter("@p0", 43),
            new Parameter("@region", "North"),
            new Parameter("@area", 1),
            ], sql.Parameters.ToArray());

        var sqlCopy = sql.GetSql();
#if NET9_0_OR_GREATER
        Assert.Same(finalSql, sqlCopy); // reused when possible
#else
        Assert.NotSame(finalSql, sqlCopy);
#endif

        sql.Clear();
        Assert.True(sql.Parameters.IsEmpty);
        Assert.Equal("", sql.GetSql());
    }

    [Fact]
    public void BasicSqlInterpolatedWithExpressionAndConflictingName()
    {
        string region = "North";
        int p1 = 42, area = 1;
        SqlBuilder sql = $"""
            select top 5 *
            from Customers
            where Id=@{p1} and Region=@{region.ToLower()} and Area=@{area}
            """;

        Assert.Equal(3, sql.Parameters.Length);
        Assert.False(sql.HasInjectedValue);

        var finalSql = sql.GetSql();
        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@p1 and Region=@p2 and Area=@area
            """, finalSql);

        Assert.Equal([
            new Parameter("@p1", 42),
            new Parameter("@p2", "north"),
            new Parameter("@area", 1),
            ], sql.Parameters.ToArray());

        var sqlCopy = sql.GetSql();
#if NET9_0_OR_GREATER
        Assert.Same(finalSql, sqlCopy); // reused when possible
#else
        Assert.NotSame(finalSql, sqlCopy);
#endif

        sql.Clear();
        Assert.True(sql.Parameters.IsEmpty);
        Assert.Equal("", sql.GetSql());
    }

    [Fact]
    public void BasicSqlInterpolatedWithInjectedValue()
    {
        string region = "North";
        int p1 = 42, area = 1;
        SqlBuilder sql = $"""
            select top 5 *
            from Customers
            where Id=@{p1} and Region=@{region.ToLower()} and Area={area}
            """;

        Assert.Equal(2, sql.Parameters.Length);
        Assert.True(sql.HasInjectedValue);

        var finalSql = sql.GetSql();
        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@p1 and Region=@p2 and Area=1
            """, finalSql);

        Assert.Equal([
            new Parameter("@p1", 42),
            new Parameter("@p2", "north"),
            ], sql.Parameters.ToArray());

        var sqlCopy = sql.GetSql();
        Assert.NotSame(finalSql, sqlCopy); // never reused because of injection

        sql.Clear();
        Assert.True(sql.Parameters.IsEmpty);
        Assert.Equal("", sql.GetSql());
    }

    [Fact]
    public void ApiUsage()
    {
        using DbConnection conn = new SqlConnection(); // not used
        string region = "North";
        int id = 42;

        var cmd = conn.Command($"""
            select top 5 *
            from Customers
            where Id=@{id} and Region=@{region}
            """);

        Assert.Equal("""
            select top 5 *
            from Customers
            where Id=@id and Region=@region
            """, cmd.ToString());

        Assert.Equal([
            new Parameter("@id", 42),
            new Parameter("@region", "North")
            ], cmd.GetParameters());

        // we would then use cmd.Execute(), for example
    }
}


#endif

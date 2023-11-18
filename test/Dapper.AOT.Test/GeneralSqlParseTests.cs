
using Dapper.Internal.SqlParsing;
using Xunit;
using static global::Dapper.SqlAnalysis.SqlSyntax;
namespace Dapper.AOT.Test;

public class GeneralSqlParseTests
{
    [Fact]
    public void BatchifyNonStrippedPostgresql() => Assert.Equal(
        [
            new("something;"),
            new("    something else;"),
            new("""
                -- comment
                ;
                """),
            new("more;"),
        ], GeneralSqlParser.Parse("""
            something;
                something else;
            -- comment
            ;
            ;
            more
            """, PostgreSql, strip: false));

    [Fact]
    public void BatchifyStrippedPostgresql() => Assert.Equal(
        [
            new("something;"),
            new("something else;"),
            new("more;"),
        ], GeneralSqlParser.Parse("""
            something;
                something else;
            -- comment
            ;
            ;
            more
            """, PostgreSql, strip: true));

    [Fact]
    public void BatchifyNonStrippedSqlServer() => Assert.Equal(
        [
            new("""
            something;
                something else;
            -- comment
            ;
            ;
            more
            """),
        ], GeneralSqlParser.Parse("""
            something;
                something else;
            -- comment
            ;
            ;
            more
            """, SqlServer, strip: false));

    [Fact]
    public void BatchifyStrippedSqlServer() => Assert.Equal(
        [
            new("""
                something;something else;more
                """),
        ], GeneralSqlParser.Parse("""
            something;
                something else;
            -- comment
            ;
            ;
            more
            """, SqlServer, strip: true));

    [Fact]
    public void BatchifyNonStrippedSqlServer_Go() => Assert.Equal(
    [
        new("something"),
        new("something ' GO ' else;"),
    ], GeneralSqlParser.Parse("""
            something
            GO
            something ' GO ' else;
            """, SqlServer, strip: true));

    [Fact]
    public void DetectArgs() => Assert.Equal(
        [
            new ("""
                select * from SomeTable where Id = @foo and Name = '@bar';
                """, new("@foo", 35)),
        ], GeneralSqlParser.Parse("""
        select * from SomeTable
        where Id = @foo and Name = '@bar'
        """, PostgreSql, strip: true));

    [Fact]
    public void DetectArgsAndBatchify() => Assert.Equal(
        [
            new("select * from SomeTable where Id = @foo and Name = '@bar';", new("@foo", 35)),
            new("insert Bar (Id, X) values ($1, @@IDENTITY);", new("$1", 27)),
            new("insert Blap (Id) values ($1, @foo);", new("$1", 25), new("@foo", 29)),
        ], GeneralSqlParser.Parse("""
        select * from SomeTable where Id = @foo and Name = '@bar' -- $4
        ;
        insert Bar (Id, X) /* @abc */ values ($1, @@IDENTITY);
        insert Blap (Id) values ($1, @foo)
        """, PostgreSql, strip: true));

    [Fact]
    public void StringEscapingSqlServer() => Assert.Equal(
        [
            new("select ' @a '' @b ' as [ @c ]] @d ];") // no vars
        ], GeneralSqlParser.Parse("""
        select ' @a '' @b ' as [ @c ]] @d ];
        """, SqlServer, strip: true));
}

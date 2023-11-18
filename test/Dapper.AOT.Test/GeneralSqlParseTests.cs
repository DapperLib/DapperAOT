using Dapper.Internal;
using Dapper.Internal.SqlParsing;
using Xunit;

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
            """, Batch.Semicolon, strip: false));

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
            """, Batch.Semicolon, strip: true));

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
            """, Batch.Go, strip: false));

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
            """, Batch.Go, strip: true));

    [Fact]
    public void BatchifyNonStrippedSqlServer_Go() => Assert.Equal(
    [
        new("something"),
        new("something ' GO ' else;"),
    ], GeneralSqlParser.Parse("""
            something
            GO
            something ' GO ' else;
            """, Batch.Go, strip: true));

    [Fact]
    public void DetectArgs() => Assert.Equal(
        [
            new ("""
                select * from SomeTable where Id = @foo and Name = '@bar';
                """, new("@foo", 35)),
        ], GeneralSqlParser.Parse("""
        select * from SomeTable
        where Id = @foo and Name = '@bar'
        """, Batch.Semicolon, strip: true));

    [Fact]
    public void DetectArgsAndBatchify() => Assert.Equal(
        [
            new("select * from SomeTable where Id = @foo and Name = '@bar';", new("@foo", 35)),
            new("insert Bar (Id) values ($1);", new("$1", 24)),
            new("insert Blap (Id) values ($1, @foo);", new("$1", 25), new("@foo", 29)),
        ], GeneralSqlParser.Parse("""
        select * from SomeTable where Id = @foo and Name = '@bar' -- $4
        ;
        insert Bar (Id) /* @abc */ values ($1);
        insert Blap (Id) values ($1, @foo)
        """, Batch.Semicolon, strip: true));
}

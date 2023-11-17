using Dapper.Internal;
using Xunit;

namespace Dapper.AOT.Test;

public class GeneralSqlParseTests
{
    [Fact]
    public void BatchifyNonStripped() => Assert.Equal(
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
        """, strip: false));

    [Fact]
    public void BatchifyStripped() => Assert.Equal(
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
        """, strip: true));

    [Fact]
    public void DetectArgs() => Assert.Equal(
[
    new("""
        select * from SomeTable where Id = @foo and Name = '@bar';
        """, "@foo"),
], GeneralSqlParser.Parse("""
        select * from SomeTable
        where Id = @foo and Name = '@bar'
        """, strip: true));

    [Fact]
    public void DetectArgsAndBatchify() => Assert.Equal(
[
    new("select * from SomeTable where Id = @foo and Name = '@bar';", "@foo"),
    new("insert Bar (Id) values ($1);", "$1"),
    new("insert Blap (Id) values ($1, @foo);", "$1", "@foo"),
], GeneralSqlParser.Parse("""
        select * from SomeTable where Id = @foo and Name = '@bar' -- $4
        ;
        insert Bar (Id) /* @abc */ values ($1);
        insert Blap (Id) values ($1, @foo)
        """, strip: true));
}

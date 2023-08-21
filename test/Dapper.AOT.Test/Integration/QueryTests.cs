using System;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(nameof(SqlClientFixture))]
public class QueryTests : IClassFixture<SqlClientFixture>
{
    private readonly SqlClientFixture Database;

    public QueryTests(SqlClientFixture database) => Database = database;

    private Command<object?> Command() => Database.Connection.Command<object?>("select 1 as Id, 'abc' as Name union all select 2, 'def'");

    [Fact]
    public async Task QueryBufferedAsync()
    {
        var rows = await Command().QueryBufferedAsync<Foo>(null, Foo.RowFactory);
        Assert.Equal(2, rows.Count);
        var obj = rows[0];
        Assert.Equal(1, obj.Id);
        Assert.Equal("abc", obj.Name);
        obj = rows[1];
        Assert.Equal(2, obj.Id);
        Assert.Equal("def", obj.Name);
    }

    public class Foo
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public static RowFactory<Foo> RowFactory => FooFactory.Instance;

        private class FooFactory : RowFactory<Foo>
        {
            public static readonly FooFactory Instance = new();
            public override object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    var name = reader.GetName(columnOffset);
                    int token;
                    if (string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase)) token = 0;
                    else if (string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase)) token = 1;
                    else token = -1;
                    tokens[i] = token;
                    columnOffset++;
                }
                return null;
            }
            public override Foo Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
            {
                var obj = new Foo();
                foreach (var token in tokens)
                {
                    switch (token)
                    {
                        case 0: obj.Id = reader.GetInt32(columnOffset); break;
                        case 1: obj.Name = reader.GetString(columnOffset); break;
                    }
                    columnOffset++;
                }
                return obj;
            }
        }
    }
}

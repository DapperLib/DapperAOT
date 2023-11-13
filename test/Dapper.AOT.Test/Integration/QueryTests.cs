using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedSqlClient.Collection)]
public class QueryTests : IDisposable
{
    private readonly SqlConnection connection;
    void IDisposable.Dispose() => connection?.Dispose();
    public QueryTests(SqlClientFixture database) => connection = database.CreateConnection();

    private Command<object?> Read(int count) => connection.Command<object?>(
        count switch
        {
            0 => "select 1 as Id, 'abc' as Name where 1 = 0",
            1 => "select 1 as Id, 'abc' as Name",
            2 => "select 1 as Id, 'abc' as Name union all select 2, 'def'",
            _ => throw new ArgumentOutOfRangeException(nameof(count)),
        });

    private void AssertClosed() => Assert.Equal(System.Data.ConnectionState.Closed, connection.State);

    private static void AssertExpectedTypedRow(Foo? row, int expected)
    {
        if (expected == 0)
        {
            Assert.Null(row);
        }
        else
        {
            Assert.NotNull(row);
            Assert.Equal(1, row!.Id);
            Assert.Equal("abc", row.Name);
        }
    }
    private static void AssertExpectedDynamicRow(dynamic? row, int expected)
    {
        if (expected == 0)
        {
            Assert.Null((object?)row);
        }
        else
        {
            Assert.NotNull((object?)row);
            Assert.Equal(1, (int)row!.Id);
            Assert.Equal("abc", (string)row.Name);
        }
    }

    private static void AssertExpectedTypedRows(List<Foo> rows, int expected)
    {
        Assert.Equal(expected, rows.Count);
        if (expected > 0)
        {
            var obj = rows[0];
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("abc", obj.Name);
            if (expected > 1)
            {
                obj = rows[1];
                Assert.NotNull(obj);
                Assert.Equal(2, obj.Id);
                Assert.Equal("def", obj.Name);
            }
        }
    }

    private static void AssertExpectedDynamicRows(List<dynamic> rows, int expected)
    {
        Assert.Equal(expected, rows.Count);
        if (expected > 0)
        {
            var obj = rows[0];
            Assert.NotNull((object)obj);
            Assert.Equal(1, (int)obj.Id);
            Assert.Equal("abc", (string)obj.Name);
            if (expected > 1)
            {
                obj = rows[1];
                Assert.NotNull((object)obj);
                Assert.Equal(2, (int)obj.Id);
                Assert.Equal("def", (string)obj.Name);
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]

    public void QueryBufferedTypedSync(int count)
    {
        AssertClosed();
        var rows = Read(count).QueryBuffered<Foo>(null, Foo.RowFactory);
        AssertClosed();
        AssertExpectedTypedRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Confidence in what is happening")]
    public void QueryNonBufferedTypedSync(int count)
    {
        AssertClosed();
        List<Foo> rows = new();
        foreach (var row in Read(count).QueryUnbuffered<Foo>(null, Foo.RowFactory))
        {
            rows.Add(row);
        }
        AssertClosed();
        AssertExpectedTypedRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryBufferedTypedAsync(int count)
    {
        AssertClosed();
        var rows = await Read(count).QueryBufferedAsync<Foo>(null, Foo.RowFactory);
        AssertClosed();
        AssertExpectedTypedRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Confidence in what is happening")]
    public async Task QueryNonBufferedTypedAsync(int count)
    {
        AssertClosed();
        List<Foo> rows = new();
        await foreach (var row in Read(count).QueryUnbufferedAsync<Foo>(null, Foo.RowFactory))
        {
            rows.Add(row);
        }
        AssertClosed();
        AssertExpectedTypedRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QueryBufferedDynamicSync(int count)
    {
        AssertClosed();
        var rows = Read(count).QueryBuffered<dynamic>(null, RowFactory.Inbuilt.Dynamic);
        AssertClosed();
        AssertExpectedDynamicRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Confidence in what is happening")]
    public void QueryNonBufferedDynamicSync(int count)
    {
        AssertClosed();
        List<dynamic> rows = new();
        foreach (var row in Read(count).QueryUnbuffered<dynamic>(null, RowFactory.Inbuilt.Dynamic))
        {
            rows.Add(row);
        }
        AssertClosed();
        AssertExpectedDynamicRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryBufferedDynamicAsync(int count)
    {
        AssertClosed();
        var rows = await Read(count).QueryBufferedAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic);
        AssertClosed();
        AssertExpectedDynamicRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Confidence in what is happening")]
    public async Task QueryNonBufferedDynamicAsync(int count)
    {
        AssertClosed();
        List<dynamic> rows = new();
        await foreach (var row in Read(count).QueryUnbufferedAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic))
        {
            rows.Add(row);
        }
        AssertClosed();
        AssertExpectedDynamicRows(rows, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QueryFirstOrDefaultTypedSync(int count)
    {
        AssertClosed();
        var row = Read(count).QueryFirstOrDefault<Foo>(null, Foo.RowFactory);
        AssertClosed();
        AssertExpectedTypedRow(row, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryFirstOrDefaultTypedAsync(int count)
    {
        AssertClosed();
        var row = await Read(count).QueryFirstOrDefaultAsync<Foo>(null, Foo.RowFactory);
        AssertClosed();
        AssertExpectedTypedRow(row, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QueryFirstTypedSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QueryFirst<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            default:
                var row = Read(count).QueryFirst<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryFirstTypedAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QueryFirstAsync<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            default:
                var row = await Read(count).QueryFirstAsync<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QuerySingleOrDefaultTypedSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
            case 1:
                var row = Read(count).QuerySingleOrDefault<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
            default:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingleOrDefault<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }

        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QuerySingleOrDefaultTypedAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
            case 1:
                var row = await Read(count).QuerySingleOrDefaultAsync<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
            default:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleOrDefaultAsync<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QuerySingleTypedSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingle<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            case 1:
                var row = Read(count).QuerySingle<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
            default:
                ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingle<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QuerySingleTypedAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleAsync<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            case 1:
                var row = await Read(count).QuerySingleAsync<Foo>(null, Foo.RowFactory);
                AssertExpectedTypedRow(row, count);
                break;
            default:
                ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleAsync<Foo>(null, Foo.RowFactory));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QueryFirstOrDefaultDynamicSync(int count)
    {
        AssertClosed();
        var row = Read(count).QueryFirstOrDefault<dynamic>(null, RowFactory.Inbuilt.Dynamic);
        AssertClosed();
        AssertExpectedDynamicRow(row, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryFirstOrDefaultDynamicAsync(int count)
    {
        AssertClosed();
        var row = await Read(count).QueryFirstOrDefaultAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic);
        AssertClosed();
        AssertExpectedDynamicRow(row, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QueryFirstDynamicSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QueryFirst<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            default:
                var row = Read(count).QueryFirst<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QueryFirstDynamicAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QueryFirstAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            default:
                var row = await Read(count).QueryFirstAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QuerySingleOrDefaultDynamicSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
            case 1:
                var row = Read(count).QuerySingleOrDefault<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
            default:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingleOrDefault<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }

        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QuerySingleOrDefaultDynamicAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
            case 1:
                var row = await Read(count).QuerySingleOrDefaultAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
            default:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleOrDefaultAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QuerySingleDynamicSync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingle<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            case 1:
                var row = Read(count).QuerySingle<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
            default:
                ex = Assert.Throws<InvalidOperationException>(() => Read(count).QuerySingle<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task QuerySingleDynamicAsync(int count)
    {
        AssertClosed();
        switch (count)
        {
            case 0:
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains no elements", ex.Message);
                break;
            case 1:
                var row = await Read(count).QuerySingleAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic);
                AssertExpectedDynamicRow(row, count);
                break;
            default:
                ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Read(count).QuerySingleAsync<dynamic>(null, RowFactory.Inbuilt.Dynamic));
                Assert.Equal("Sequence contains more than one element", ex.Message);
                break;
        }
        AssertClosed();
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

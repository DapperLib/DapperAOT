using Microsoft.Data.SqlClient;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedSqlClient.Collection)]
public class ManualGridReaderTests : IDisposable
{
    private readonly SqlConnection connection;
    void IDisposable.Dispose() => connection?.Dispose();
    public ManualGridReaderTests(SqlClientFixture database) => connection = database.CreateConnection();

    private void AssertClosed() => Assert.Equal(System.Data.ConnectionState.Closed, connection.State);

    const string SQL = """
        select 123;
        select 'abc';
        select 'def' union select null union select 'ghi';
        select 456 union select 789;
        """;
    [Fact]
    public void VanillaUsage()
    {
        AssertClosed();
        using var reader = connection.QueryMultiple(SQL);
        Assert.NotNull(reader.Command);
        Assert.Equal(123, reader.ReadSingle<int>());
        Assert.Equal("abc", reader.ReadSingle<string>());
        Assert.Equal(new[] { null, "def", "ghi" }, reader.Read<string?>(buffered: true).ToArray());
        Assert.Equal([456, 789], reader.Read<int>(buffered: false).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }

    [Fact]
    public async Task VanillaUsageAsync()
    {
        AssertClosed();
#if NET5_0_OR_GREATER
        await
#endif
        using var reader = await connection.QueryMultipleAsync(SQL);
        Assert.NotNull(reader.Command);
        Assert.Equal(123, await reader.ReadSingleAsync<int>());
        Assert.Equal("abc", await reader.ReadSingleAsync<string>());
        Assert.Equal(new[] { null, "def", "ghi" }, (await reader.ReadAsync<string?>(buffered: true)).ToArray());
        Assert.Equal([456, 789], (await reader.ReadAsync<int>(buffered: false)).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }

    [Fact]
    public void BasicUsage()
    {
        AssertClosed();
        using SqlMapper.GridReader reader = new AotGridReader(connection.Command<object?>(SQL).ExecuteReader<AotWrappedDbDataReader>(null));
        Assert.NotNull(reader.Command);
        Assert.Equal(123, ((AotGridReader)reader).ReadSingle(RowFactory.Inbuilt.Value<int>()));
        Assert.Equal("abc", ((AotGridReader)reader).ReadSingle(RowFactory.Inbuilt.Value<string>()));
        Assert.Equal(new[] { null, "def", "ghi" }, ((AotGridReader)reader).Read(buffered: true, RowFactory.Inbuilt.Value<string>()).ToArray());
        Assert.Equal([456, 789], ((AotGridReader)reader).Read(buffered: false, RowFactory.Inbuilt.Value<int>()).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }

    [Fact]
    public async Task BasicUsageAsync()
    {
        AssertClosed();
#if NET5_0_OR_GREATER
        await
#endif
        using SqlMapper.GridReader reader = new AotGridReader(await connection.Command<object?>(SQL).ExecuteReaderAsync<AotWrappedDbDataReader>(null));
        Assert.NotNull(reader.Command);
        Assert.Equal(123, await ((AotGridReader)reader).ReadSingleAsync(RowFactory.Inbuilt.Value<int>()));
        Assert.Equal("abc", await ((AotGridReader)reader).ReadSingleAsync(RowFactory.Inbuilt.Value<string>()));
        Assert.Equal(new[] { null, "def", "ghi" }, (await ((AotGridReader)reader).ReadAsync(buffered: true, RowFactory.Inbuilt.Value<string>())).ToArray());
        Assert.Equal([456, 789], (await ((AotGridReader)reader).ReadAsync(buffered: false, RowFactory.Inbuilt.Value<int>())).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }

    [Fact]
    public void BasicUsageWithOnTheFlyIdentity()
    {
        AssertClosed();
        using SqlMapper.GridReader reader = new AotGridReader(connection.Command<object?>(SQL).ExecuteReader<AotWrappedDbDataReader>(null));
        Assert.NotNull(reader.Command);
        Assert.Equal(123, reader.ReadSingle<int>());
        Assert.Equal("abc", reader.ReadSingle<string>());
        Assert.Equal(new[] { null, "def", "ghi" }, reader.Read<string>(buffered: true).ToArray());
        Assert.Equal([456, 789], reader.Read<int>(buffered: false).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }

    [Fact]
    public async Task BasicUsageWithOnTheFlyIdentityAsync()
    {
        AssertClosed();
#if NET5_0_OR_GREATER
        await
#endif
        using SqlMapper.GridReader reader = new AotGridReader(await connection.Command<object?>(SQL).ExecuteReaderAsync<AotWrappedDbDataReader>(null));
        Assert.NotNull(reader.Command);
        Assert.Equal(123, await reader.ReadSingleAsync<int>());
        Assert.Equal("abc", await reader.ReadSingleAsync<string>());
        Assert.Equal(new[] { null, "def", "ghi" }, (await reader.ReadAsync<string>(buffered: true)).ToArray());
        Assert.Equal([456, 789], (await reader.ReadAsync<int>(buffered: false)).ToArray());
        Assert.True(reader.IsConsumed);
        AssertClosed();
    }
}

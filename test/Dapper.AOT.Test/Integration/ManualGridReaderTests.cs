using System.Linq;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(nameof(SqlClientFixture))]
public class ManualGridReaderTests : IClassFixture<SqlClientFixture>
{
    private readonly SqlClientFixture Database;
    public ManualGridReaderTests(SqlClientFixture database) => Database = database;

    const string SQL = """
        select 123;
        select 'abc';
        select 'def' union select null union select 'ghi';
        select 456 union select 789;
        """;
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Clearer for test")]
    public void VanillaUsage()
    {
        using var reader = Database.Connection.QueryMultiple(SQL);
        Assert.Equal(123, reader.ReadSingle<int>());
        Assert.Equal("abc", reader.ReadSingle<string>());
        Assert.Equal(new[] { null, "def", "ghi" }, reader.Read<string?>(buffered: true).ToArray());
        Assert.Equal(new[] { 456, 789 }, reader.Read<int>(buffered: false).ToArray());
        Assert.True(reader.IsConsumed);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Clearer for test")]
    public void BasicUsage()
    {
        using SqlMapper.GridReader reader = new AotGridReader(Database.Connection.Command<object?>(SQL).ExecuteReader(null));
        Assert.Equal(123, ((AotGridReader)reader).ReadSingle(RowFactory.Inbuilt.Value<int>()));
        Assert.Equal("abc", ((AotGridReader)reader).ReadSingle(RowFactory.Inbuilt.Value<string>()));
        Assert.Equal(new[] { null, "def", "ghi" }, ((AotGridReader)reader).Read(buffered: true, RowFactory.Inbuilt.Value<string>()).ToArray());
        Assert.Equal(new[] { 456, 789 }, ((AotGridReader)reader).Read(buffered: false, RowFactory.Inbuilt.Value<int>()).ToArray());
        Assert.True(reader.IsConsumed);
    }
}

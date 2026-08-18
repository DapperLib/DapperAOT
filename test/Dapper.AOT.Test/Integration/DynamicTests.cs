using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedSqlClient.Collection)]
public class DynamicTests : IDisposable
{
    private readonly SqlConnection connection;
    void IDisposable.Dispose() => connection?.Dispose();
    public DynamicTests(SqlClientFixture database) => connection = database.CreateConnection();

    [Fact]
    public void CanAccessDynamicData()
    {
        var wilma = connection.Command("select * from " + SqlClientFixture.AotIntegrationDynamicTests + " where Name = 'Wilma';", handler: CommandFactory.Simple)
            .QuerySingle(null, RowFactory.Inbuilt.Dynamic);
        Assert.NotNull(wilma);
        Assert.Equal("Wilma", (string)wilma.Name);
        Assert.True((int)wilma.Id > 0);
        Assert.Throws<KeyNotFoundException>(() => _ = wilma.NotExist);

        // dynamic records are mutable, like vanilla Dapper's DapperRow: an existing
        // member can be replaced, and a new member added
        wilma.Name = "abc";
        Assert.Equal("abc", (string)wilma.Name);
        wilma.NotExist = 123;
        Assert.Equal(123, (int)wilma.NotExist);

        IDictionary<string, object?> lookup = wilma;
        Assert.True(lookup.Remove("NotExist"));
        Assert.False(lookup.Remove("NotExist"));
        Assert.Throws<KeyNotFoundException>(() => _ = wilma.NotExist);
    }
}


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

    [SkippableFact]
    public void CanAccessDynamicData()
    {
        var wilma = connection.Command("select * from " + SqlClientFixture.AotIntegrationDynamicTests + " where Name = 'Wilma';", handler: CommandFactory.Simple)
            .QuerySingle(null, RowFactory.Inbuilt.Dynamic);
        Assert.NotNull(wilma);
        Assert.Equal("Wilma", (string)wilma.Name);
        Assert.True((int)wilma.Id > 0);
        Assert.Throws<KeyNotFoundException>(() => _ = wilma.NotExist);
        Assert.Throws<NotSupportedException>(() => wilma.Name = "abc");
    }
}


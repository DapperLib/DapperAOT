using System;
using System.Collections.Generic;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(nameof(SqlClientFixture))]
public class DynamicTests : IClassFixture<SqlClientFixture>
{
    private readonly SqlClientFixture Database;
    public DynamicTests(SqlClientFixture database) => Database = database;

    [Fact]
    public void CanAccessDynamicData()
    {
        var wilma = Database.Connection.Command("select * from AotIntegrationDynamicTests where Name = 'Wilma';", handler: CommandFactory.Simple)
            .QuerySingle(null, RowFactory.Inbuilt.Dynamic);
        Assert.NotNull(wilma);
        Assert.Equal("Wilma", (string)wilma.Name);
        Assert.True((int)wilma.Id > 0);
        Assert.Throws<KeyNotFoundException>(() => _ = wilma.NotExist);
        Assert.Throws<NotSupportedException>(() => wilma.Name = "abc");
    }
}


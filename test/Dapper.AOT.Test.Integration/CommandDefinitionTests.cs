using System;
using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode;
using Dapper.AOT.Test.Integration.Setup;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class CommandDefinitionTests : IntegrationTestsBase
{
    public CommandDefinitionTests(PostgresqlFixture fixture) : base(fixture)
    {
    }

    protected override void SetupDatabase(IDbConnection dbConnection)
    {
        base.SetupDatabase(dbConnection);
        
        dbConnection.Execute($"""
            CREATE TABLE IF NOT EXISTS {CommandDefinitionPoco.TableName}(
                id     integer PRIMARY KEY,
                name   varchar(40)
            );

            TRUNCATE {CommandDefinitionPoco.TableName};
            
            INSERT INTO {CommandDefinitionPoco.TableName} (id, name)
            VALUES (1, 'my-data'),
                   (2, 'my-poco'),
                   (3, 'your-data');
        """);
    }

    [Fact]
    public void CommandDefinition_BasicUsage_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<CommandDefinitionUsage, CommandDefinitionPoco>(DbConnection);
        
        Assert.NotNull(result);
        Assert.True(result.Id.Equals(1));
        Assert.Equal("my-data", result.Name);
    }
}
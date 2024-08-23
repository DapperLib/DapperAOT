using System;
using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode;
using Dapper.AOT.Test.Integration.Setup;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class DbStringTests : IntegrationTestsBase
{
    public DbStringTests(PostgresqlFixture fixture) : base(fixture)
    {
    }

    protected override void SetupDatabase(IDbConnection dbConnection)
    {
        base.SetupDatabase(dbConnection);
        
        dbConnection.Execute($"""
            CREATE TABLE IF NOT EXISTS {DbStringPoco.TableName}(
                id     integer PRIMARY KEY,
                name   varchar(40)
            );

            TRUNCATE {DbStringPoco.TableName};
            
            INSERT INTO {DbStringPoco.TableName} (id, name)
            VALUES (1, 'my-dbString'),
                   (2, 'my-poco'),
                   (3, 'your-data');
        """);
    }

    [Fact]
    public void DbString_BasicUsage_InterceptsAndReturnsExpectedData()
    {
        var result = ExecuteInterceptedUserCode<DbStringUsage, DbStringPoco>(DbConnection);
        
        Assert.True(result.Id.Equals(1));
        Assert.True(result.Name.Equals("my-dbString", StringComparison.InvariantCultureIgnoreCase));
    }
}
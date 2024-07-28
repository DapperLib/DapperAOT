using System.Data;
using Dapper.AOT.Test.Integration.Executables.Models;
using Dapper.AOT.Test.Integration.Executables.UserCode;
using Xunit;

namespace Dapper.AOT.Test.Integration.Tests;

[Collection(SharedPostgresqlClient.Collection)]
public class DbStringIntegrationTests : InterceptorsIntegratedTestsBase
{
    public DbStringIntegrationTests(PostgresqlFixture fixture)
        : base(fixture)
    {
    }
    
    [Fact]
    public void SimpleDbString_RunsAgainstDatabase()
    {
        var result = ExecuteUserCode<DbStringUsage, DbStringPoco>();
        Assert.NotNull(result);
    }

    protected override void SetupDatabase(IDbConnection dbConnection) => dbConnection.Execute("""
        CREATE TABLE IF NOT EXISTS dbString_test(
             id     integer PRIMARY KEY,
             name   varchar(40) NOT NULL
        );
        TRUNCATE dbString_test;

        INSERT INTO dbString_test(id, name)
        VALUES 
            (1, 'my-string'),
            (2, 'another-string');
    """);
}
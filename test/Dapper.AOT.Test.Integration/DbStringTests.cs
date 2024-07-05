using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper.AOT.Test.Integration.Setup;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class DbStringTests : InterceptedCodeExecutionTestsBase
{
    public DbStringTests(PostgresqlFixture fixture, ITestOutputHelper log) : base(fixture, log)
    {
        Fixture.NpgsqlConnection.Execute("""
            CREATE TABLE IF NOT EXISTS dbStringTestsTable(
                id     integer PRIMARY KEY,
                name   varchar(40) NOT NULL
            );
            TRUNCATE dbStringTestsTable;
        """);
    }
    
    [Fact]
    [DapperAot]
    public async Task Test()
    {
        await Fixture.NpgsqlConnection.ExecuteAsync("""
            INSERT INTO dbStringTestsTable(id, name)
            VALUES (1, 'me testing!')
        """);
        
        var sourceCode = PrepareSourceCodeFromFile("DbString");
        var executionResults = BuildAndExecuteInterceptedUserCode<InterceptionExecutables.IncludedTypes.Poco>(sourceCode, methodName: "Execute");
        
        Assert.NotNull(executionResults);
        // TODO check that stack trace contains call to `Dapper.Aot.Generated.DbStringHelpers`. probably can be done like here https://stackoverflow.com/a/33939304
    }
}
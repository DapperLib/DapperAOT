using System.Data;
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
            CREATE TABLE IF NOT EXISTS dbStringTable(
                id     integer PRIMARY KEY,
                name   varchar(40) NOT NULL CHECK (name <> '')
            );
            TRUNCATE dbStringTable;
        """);
    }
    
    [Fact]
    [DapperAot]
    public async Task Test()
    {
        var sourceCode = PrepareSourceCodeFromFile("DbString");
        var executionResults = BuildAndExecuteInterceptedUserCode<int>(sourceCode, methodName: "ExecuteAsync");
         
        // TODO DO THE CHECK HERE
    }
}
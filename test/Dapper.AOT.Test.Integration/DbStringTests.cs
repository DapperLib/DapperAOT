using System.Linq;
using Dapper.AOT.Test.Integration.Setup;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class DbStringTests
{
    private PostgresqlFixture _fixture;
    
    public DbStringTests(PostgresqlFixture fixture)
    {
        _fixture = fixture;
        fixture.NpgsqlConnection.Execute("""
            CREATE TABLE IF NOT EXISTS dbStringTable(
                id     integer PRIMARY KEY,
                name   varchar(40) NOT NULL CHECK (name <> '')
            );
            TRUNCATE dbStringTable;
        """
        );
    }
    
    [Fact]
    public void ExecuteMulti()
    {
        
    }
}
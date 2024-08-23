using Npgsql;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace Dapper.AOT.Test.Integration.Setup;

[CollectionDefinition(Collection)]
public class SharedPostgresqlClient : ICollectionFixture<PostgresqlFixture>
{
    public const string Collection = nameof(SharedPostgresqlClient);
}

public sealed class PostgresqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    private NpgsqlConnection? _sharedConnection;

    public NpgsqlConnection NpgsqlConnection
        => _sharedConnection ??= CreateOpenConnection();

    public NpgsqlConnection CreateOpenConnection()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (_postgresContainer)
        {
            var tmp = _sharedConnection;
            _sharedConnection = null;
            if (tmp is not null)
            {
                await tmp.DisposeAsync();
            }
        }
    }
}

using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[CollectionDefinition(Collection)]
public class SharedMsSqlClient : ICollectionFixture<MsSqlFixture>
{
    public const string Collection = nameof(SharedMsSqlClient);
}

public sealed class MsSqlFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .Build();

    public string ConnectionString { get; private set; } = null!;

    private SqlConnection? _sharedConnection;

    public SqlConnection MsSqlConnection
        => _sharedConnection ??= CreateOpenConnection();

    public SqlConnection CreateOpenConnection()
    {
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        ConnectionString = _msSqlContainer.GetConnectionString();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using (_msSqlContainer)
        {
            var tmp = _sharedConnection;
            _sharedConnection = null;
            if (tmp is not null)
            {
                tmp.Close();
#if NET6_0_OR_GREATER
                await tmp.DisposeAsync();
#endif
            }
        }
    }
}

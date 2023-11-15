
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

[module:DapperAot]

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = ".",
    InitialCatalog = "master",
    IntegratedSecurity = true,
    TrustServerCertificate = true,
}.ConnectionString;

var obj = new BenchRunner(connectionString, SqlClientFactory.Instance);
obj.Create();
await obj.LoadMultipleUpdatesRows(50);

class BenchRunner
{
    private readonly string _connectionString;
    private readonly DbProviderFactory _dbProviderFactory;
    public BenchRunner(string connectionString, DbProviderFactory dbProviderFactory)
    {
        _dbProviderFactory = dbProviderFactory;
        _connectionString = connectionString;
    }

    public void Create()
    {
        using var conn = _dbProviderFactory.CreateConnection();
        conn.ConnectionString = _connectionString;

        try { conn.Execute("DROP TABLE world;"); } catch { }
        conn.Execute("CREATE TABLE world (id int not null primary key, randomNumber int not null);");
        conn.Execute("TRUNCATE TABLE world;");
        conn.Execute("INSERT World (id, randomNumber) VALUES (@Id, @RandomNumber)", Invent(10000));

        static IEnumerable<World> Invent(int count)
        {
            var rand = GetRandom();
            return Enumerable.Range(1, 10000).Select(i => new World { Id = i, RandomNumber = rand.Next() });
        }
    }

    public async Task<World[]> LoadMultipleUpdatesRows(int count)
    {
        count = Clamp(count, 1, 500);

        var parameters = new Dictionary<string, object>();

        using var db = _dbProviderFactory.CreateConnection();

        db!.ConnectionString = _connectionString;
        await db.OpenAsync();

        var results = new World[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = await ReadSingleRow(db);
        }

        var rand = GetRandom();
        for (var i = 0; i < count; i++)
        {
            var randomNumber = rand.Next(1, 10001);
            parameters[$"@Rn_{i}"] = randomNumber;
            parameters[$"@Id_{i}"] = results[i].Id;

            results[i].RandomNumber = randomNumber;
        }

        await db.ExecuteAsync(BatchUpdateString.Query(count), parameters);
        return results;
    }

    // note that this QueryFirstOrDefaultAsync<struct> is unusual, hence DAP038
    // see: https://aot.dapperlib.dev/rules/DAP038
    //
    // options:
    // 0. leave it as-is
    // 1. use QueryFirstAsync and eat the exception if no rows
    // 2  use QueryFirstOrDefaultAsync<World?> which allows `null` to be expressed
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Library", "DAP038:Value-type single row 'OrDefault' usage", Justification = "Retain old behaviour for baseline")]
    static Task<World> ReadSingleRow(DbConnection db)
    {
        return db.QueryFirstOrDefaultAsync<World>(
                "SELECT id, randomnumber FROM world WHERE id = @Id",
                new { Id = GetRandom().Next(1, 10001) });
    }

    static Random GetRandom()
    {
#if NET6_OR_GREATER
        return Random.Shared;
#else
        return new Random();
#endif
    }

    static int Clamp(int value, int min, int max)
    {
#if NET6_OR_GREATER
        return Math.Clamp(value, min, max);
#else
        if (value < min) value = min;
        if (value > max) value = max;
        return value;
#endif
    }
}

public struct World
{
    public int Id { get; set; }

    public int RandomNumber { get; set; }
}


internal class BatchUpdateString
{
    private const int MaxBatch = 500;

    private static readonly string[] _queries = new string[MaxBatch + 1];

    public static string Query(int batchSize)
    {
        if (_queries[batchSize] != null)
        {
            return _queries[batchSize];
        }

        var lastIndex = batchSize - 1;

        var sb = new StringBuilder();

        sb.Append("UPDATE world SET randomNumber = temp.randomNumber FROM (VALUES ");
        Enumerable.Range(0, lastIndex).ToList().ForEach(i => sb.Append($"(@Id_{i}, @Rn_{i}), "));
        sb.Append($"(@Id_{lastIndex}, @Rn_{lastIndex}) ORDER BY 1) AS temp(id, randomNumber) WHERE temp.id = world.id");

        return _queries[batchSize] = sb.ToString();
    }
}
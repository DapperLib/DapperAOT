using Dapper;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

[module: DapperAot]

public static class Foo
{
    static async Task SomeCode(DbConnection connection, string bar)
    {
        var obj = new { Foo = 12, bar };
        _ = connection.ExecuteScalar("def", obj);
        _ = connection.ExecuteScalar("def", commandType: CommandType.StoredProcedure);
        _ = connection.ExecuteScalar("def", commandType: CommandType.Text);

        _ = connection.ExecuteScalar<float>("def", obj);
        _ = connection.ExecuteScalar<float>("def", commandType: CommandType.StoredProcedure);
        _ = connection.ExecuteScalar<float>("def", commandType: CommandType.Text);

        _ = await connection.ExecuteScalarAsync("def", obj);
        _ = await connection.ExecuteScalarAsync("def", commandType: CommandType.StoredProcedure);
        _ = await connection.ExecuteScalarAsync("def", commandType: CommandType.Text);

        _ = await connection.ExecuteScalarAsync<float>("def", obj);
        _ = await connection.ExecuteScalarAsync<float>("def", commandType: CommandType.StoredProcedure);
        _ = await connection.ExecuteScalarAsync<float>("def", commandType: CommandType.Text);
    }
}
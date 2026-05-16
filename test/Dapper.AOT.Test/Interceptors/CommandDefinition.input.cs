using Dapper;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading;

[module: DapperAot]

public static class Foo
{
    static void Run(DbConnection connection)
    {
        var sql = "sp_crunch";

       // 0: sql comes as local var
       _ = connection.ExecuteScalar<string>(new CommandDefinition(
           sql,
           new { X = 3 },
           commandType: CommandType.StoredProcedure,
           flags: CommandFlags.Buffered,
           cancellationToken: CancellationToken.None));

        // 1: sql inline
        _ = connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "sp_crunch",
            new { X = 3 },
            commandType: CommandType.StoredProcedure,
            flags: CommandFlags.Buffered,
            cancellationToken: CancellationToken.None));

        // 2: very limited setupdap
        _ = connection.ExecuteScalarAsync<string>(new CommandDefinition(
            "select * from table where X = @X",
            new { X = 3 })
        );

        // 3: no async
        _ = connection.ExecuteScalar<string>(new CommandDefinition(
            "select * from table where X = @X",
            new { X = 3 })
        );

        // 4: command definition as local var
        var local = new CommandDefinition(
            "select * from table where X = @X",
            new { X = 3 }
        );
        _ = connection.ExecuteScalarAsync<string>(local);

        // 5: query via command definition
        var results = connection.Query<string>(
            new CommandDefinition(
                commandText: "select * from table where name = @name",
                parameters: new
                {
                    name = "my-data"
                },
                cancellationToken: CancellationToken.None)
        );
    }
}
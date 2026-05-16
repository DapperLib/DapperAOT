using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Dapper.AOT.Test.Integration.Executables.Models;

namespace Dapper.AOT.Test.Integration.Executables.UserCode;

[DapperAot]
public class CommandDefinitionUsage : IExecutable<CommandDefinitionPoco>
{
    public CommandDefinitionPoco Execute(IDbConnection connection)
    {
        var results = connection.Query<CommandDefinitionPoco>(
            new CommandDefinition(
                commandText: $"select * from {CommandDefinitionPoco.TableName} where name = @name",
                parameters: new
                {
                    name = "my-data"
                },
                cancellationToken: CancellationToken.None)
        );

        return results.First();
    }
}
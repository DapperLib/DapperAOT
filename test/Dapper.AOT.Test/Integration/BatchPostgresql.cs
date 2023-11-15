using System.Data;
using System.Linq;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class BatchPostgresql
{
    private PostgresqlFixture _fixture;

    public BatchPostgresql(PostgresqlFixture fixture)
    {
        _fixture = fixture;
        fixture.NpgsqlConnection.Execute("""
            CREATE TABLE IF NOT EXISTS batch_exec(
                 id     integer PRIMARY KEY,
                 name   varchar(40) NOT NULL CHECK (name <> '')
            );
            TRUNCATE batch_exec;
            """);
    }

    [Fact]
    public void ExecuteMulti()
    {
        var items = Enumerable.Range(0, 100).Select(i => (i, $"item {i}")).ToList();
        var count = _fixture.NpgsqlConnection.Command("""
                INSERT INTO batch_exec(id, name)
                VALUES (@x, @y);
                """, CommandType.Text, handler: CustomHandler.Instance).Execute(items);

        Assert.Equal(100, count);

        var actual = _fixture.NpgsqlConnection.QuerySingle<int>("""
            select count(1) from batch_exec
            """);
        Assert.Equal(100, actual);
    }

    private class CustomHandler : CommandFactory<(int x, string y)>
    {
        public static readonly CustomHandler Instance = new();
        private CustomHandler() { }

        public override void AddParameters(in UnifiedCommand command, (int x, string y) args)
        {
            var ps = command.Parameters;
            var p = command.CreateParameter();
            p.DbType = DbType.Int32;
            p.Direction = ParameterDirection.Input;
            p.Value = AsValue(args.x);
            p.ParameterName = "x";
            ps.Add(p);

            p = command.CreateParameter();
            p.DbType = DbType.AnsiString;
            p.Size = 40;
            p.Direction = ParameterDirection.Input;
            p.Value = AsValue(args.y);
            p.ParameterName = "y";
            ps.Add(p);
        }

        public override void UpdateParameters(in UnifiedCommand command, (int x, string y) args)
        {
            var ps = command.Parameters;
            ps[0].Value = AsValue(args.x);
            ps[1].Value = AsValue(args.y);
        }

        public override bool CanPrepare => true;
    }
}

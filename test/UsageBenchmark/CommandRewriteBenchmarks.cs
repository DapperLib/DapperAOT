using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Xml.Linq;
using Testcontainers.PostgreSql;

namespace Dapper;

[ShortRunJob, MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), CategoriesColumn]
public class CommandRewriteBenchmarks : IAsyncDisposable
{
    private readonly NpgsqlConnection npgsql = new();

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();


    public CommandRewriteBenchmarks()
    {
        _postgresContainer.StartAsync().GetAwaiter().GetResult(); // yes, I know
        npgsql.ConnectionString = _postgresContainer.GetConnectionString();
        npgsql.Open();
        npgsql.Execute("""
            CREATE TABLE IF NOT EXISTS RewriteCustomers(
                 Id     integer GENERATED ALWAYS AS IDENTITY,
                 Name   varchar(40) NOT NULL
            );
            """);
    }

    [GlobalSetup]
    public void Setup()
    {
        npgsql.Execute("TRUNCATE RewriteCustomers RESTART IDENTITY;");
    }

    public class MyArgsType
    {
        public string? Name0 { get; set; }
        public string? Name1 { get; set; }
        public string? Name2 { get; set; }
        public string? Name3 { get; set; }
    }

    public static readonly MyArgsType Args = new MyArgsType {  Name0 = "abc", Name1 = "def", Name2 ="ghi", Name3 = "jkl" };

    // for our test, we're going to do 4 operations and try rewriting it as batch
    public const string BasicSql = """
        insert into RewriteCustomers (Name) values (@Name0);
        insert into RewriteCustomers (Name) values (@Name1);
        insert into RewriteCustomers (Name) values (@Name2);
        insert into RewriteCustomers (Name) values (@Name3);
        """;

    [Benchmark(Baseline = true)]
    public int Dapper() => npgsql.Execute(BasicSql, Args);

    // note these are hand written
    [Benchmark]
    public int DapperAOT_Simple() => npgsql.Command<MyArgsType>(BasicSql, handler: BasicCommand.Instance).Execute(Args);

    [Benchmark]
    public int DapperAOT_Rewrite() => npgsql.Command<MyArgsType>(BasicSql, handler: RewriteCommand.Instance).Execute(Args);

    public async ValueTask DisposeAsync()
    {
        await npgsql.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private sealed class BasicCommand : CommandFactory<MyArgsType>
    {
        private BasicCommand() { }
        public static BasicCommand Instance { get; } = new BasicCommand();

        public override void AddParameters(in UnifiedCommand command, MyArgsType args)
        {
            var ps = command.Parameters;

            var p = command.CreateParameter();
            p.ParameterName = "Name0";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name0);
            ps.Add(p);

            p = command.CreateParameter();
            p.ParameterName = "Name1";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name1);
            ps.Add(p);

            p = command.CreateParameter();
            p.ParameterName = "Name2";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name2);
            ps.Add(p);

            p = command.CreateParameter();
            p.ParameterName = "Name3";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name3);
            ps.Add(p);
        }
    }

    private sealed class RewriteCommand : CommandFactory<MyArgsType>
    {
        private RewriteCommand() { }
        public static RewriteCommand Instance { get; } = new RewriteCommand();

        public override void AddParameters(in UnifiedCommand command, MyArgsType args)
            => throw new NotSupportedException(); // we don't expect to get here (in reality, we would have both versions)

        public override bool UseBatch(string sql) => sql == BasicSql; // assert that we're doing the right thing

        public override void AddCommands(in UnifiedBatch batch, string sql, MyArgsType args)
        {
            // the first command is initialized automatically
            batch.CommandText = "insert into RewriteCustomers(Name) values($1)";
            var p = new NpgsqlParameter(); // batch.AddParameter(); // temp hack because CanCreateParameter => false
            batch.Parameters.Add(p);
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name0);

            // the fact that the NpgSql command is the same is a coincidence of the test
            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = new NpgsqlParameter(); // batch.AddParameter();
            batch.Parameters.Add(p);
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name1);

            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = new NpgsqlParameter(); // batch.AddParameter();
            batch.Parameters.Add(p);
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name2);

            batch.AddCommand("insert into RewriteCustomers(Name) values($1)");
            p = new NpgsqlParameter(); // batch.AddParameter();
            batch.Parameters.Add(p);
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(args.Name3);
        }
    }
}
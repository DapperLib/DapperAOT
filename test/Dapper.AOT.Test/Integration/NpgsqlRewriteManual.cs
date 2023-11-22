#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public class NpgsqlRewriteManual
{
    private readonly PostgresqlFixture _fixture;

    public NpgsqlRewriteManual(PostgresqlFixture fixture)
    {
        _fixture = fixture;
        fixture.NpgsqlConnection.Execute("""
            CREATE TABLE IF NOT EXISTS rewrite_test(
                 id     integer PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
                 name   varchar(40) NOT NULL CHECK (name <> '')
            );
            TRUNCATE rewrite_test;
            """);
    }
    
    const string CompositeQuery = """
        TRUNCATE rewrite_test RESTART IDENTITY;
        
        INSERT INTO rewrite_test(name)
        VALUES (@x);
        
        INSERT INTO rewrite_test(name)
        VALUES (@x || @y);
        
        INSERT INTO rewrite_test(name)
        VALUES (@y);

        select id, name
        from rewrite_test
        order by id
        """;
    private static readonly object CompositeArgs = new { x = "abc", y = "def" };

    private static void AssertResults(IEnumerable<RewriteTestRow> results)
    {
        var list = results.AsList();
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("abc", list[0].Name);
        Assert.Equal(2, list[1].Id);
        Assert.Equal("abcdef", list[1].Name);
        Assert.Equal(3, list[2].Id);
        Assert.Equal("def", list[2].Name);
    }
    public record struct RewriteTestRow(int Id, string Name);

    [Fact]
    public void DapperVanillaConsistency()
        => AssertResults(_fixture.NpgsqlConnection.Query<RewriteTestRow>(CompositeQuery, CompositeArgs));

    [Fact]
    public void ManuallyImplementedBasic()
        => AssertResults(_fixture.NpgsqlConnection
            .Command(CompositeQuery, CommandType.Text, handler: BasicCommandFactory.Instance)
            .Query(CompositeArgs, true, rowFactory: BasicRowFactory.Instance));

    [Fact]
    public void ManuallyImplementedFancy()
    => AssertResults(_fixture.NpgsqlConnection
        .Command(CompositeQuery, CommandType.Text, handler: FancyCommandFactory.Instance)
        .Query(CompositeArgs, true, rowFactory: BasicRowFactory.Instance));

    sealed class BasicCommandFactory : CommandFactory<object>
    {
        public static BasicCommandFactory Instance { get; } = new();
        private BasicCommandFactory() { }

        public override void AddParameters(in UnifiedCommand command, object args)
        {
            var typed = Cast(args, static () => new { x = "abc", y = "def" });
            var ps = command.Parameters;
            DbParameter p = command.AddParameter();
            p.ParameterName = "x";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.x);

            p = command.AddParameter();
            p.ParameterName = "y";
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.y);
        }

        public override void UpdateParameters(in UnifiedCommand command, object args)
        {
            var typed = Cast(args, static () => new { x = "abc", y = "def" });
            var ps = command.Parameters;
            ps[0].Value = AsValue(typed.x);
            ps[1].Value = AsValue(typed.y);
        }
    }

    sealed class BasicRowFactory : RowFactory<RewriteTestRow>
    {
        public static BasicRowFactory Instance { get; } = new();

        // note we're ignoring tokenize etc for simplicity; just do raw
        public override RewriteTestRow Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
            => new(reader.GetInt32(columnOffset), reader.GetString(columnOffset + 1));
    }

    sealed class FancyCommandFactory : CommandFactory<object>
    {
        // trying to prove the feature for https://github.com/DapperLib/DapperAOT/issues/78
        public static FancyCommandFactory Instance { get; } = new();
        private FancyCommandFactory() { }

        // assert that the SQL is what we expected; we do *not* want to parse and split
        // SQL at runtime, so we only do this if we already figured out how to do it
        // (otherwise, we'll defer to the underlying ADO.NET provider)
        public override bool UseBatch(string sql) => sql == CompositeQuery;

        public override void AddCommands(in UnifiedBatch batch, string sql, object args)
        {
            var typed = Cast(args, static () => new { x = "abc", y = "def" });

            batch.AddCommand("""
                TRUNCATE rewrite_test RESTART IDENTITY
                """);

            // note: not allowed to reuse parameters between commands; throws if you try

            batch.AddCommand("""
                INSERT INTO rewrite_test(name)
                VALUES ($1)
                """);

            var p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.x);

            batch.AddCommand("""
                INSERT INTO rewrite_test(name)
                VALUES ($1 || $2)
                """);

            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.x);

            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.y);

            batch.AddCommand("""
                INSERT INTO rewrite_test(name)
                VALUES ($1)
                """);

            p = batch.AddParameter();
            p.DbType = DbType.String;
            p.Size = -1;
            p.Value = AsValue(typed.y);

            batch.AddCommand("""
                select id, name
                from rewrite_test
                order by id
                """);
        }

        public override void PostProcess(in UnifiedBatch batch, object args, int rowCount)
        {
            // example usage
            // args.Something = Cast<int>(commands[commandIndex].Parameters[1].Value);
        }

        public override bool RequirePostProcess => true;

        public override void PostProcess(in UnifiedCommand command, object args, int rowCount)
            => throw new NotImplementedException("we don't expect to get here!");

        public override void AddParameters(in UnifiedCommand command, object args)
            => throw new NotImplementedException("we don't expect to get here!");

        public override void UpdateParameters(in UnifiedCommand command, object args)
            => throw new NotImplementedException("we don't expect to get here!");
    }
}


#endif
using Dapper.Internal.SqlParsing;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Dapper.AOT.Test;
// inspired, with love, from https://github.com/npgsql/npgsql/blob/main/test/Npgsql.Tests/SqlQueryParserTests.cs

public class NpgsqlParseTests
{
    [Fact]
    public void Parameter_simple()
    {
        var result = ParseCommand("SELECT :p1, :p2", [(":p1", "foo"), (":p2", "bar")]).Single();
        Assert.Equal("SELECT $1, $2", result.FinalCommandText);
        Assert.Equal(Args(("$1", "foo"), ("$2", "bar")), result.Parameters);
    }

    static (string name, object value)[] Args(params (string name, object value)[] args) => args;

    [Fact]
    public void Parameter_name_with_dot()
    {
        var p = (":a.parameter", "foo");
        var results = ParseCommand("INSERT INTO data (field_char5) VALUES (:a.parameter)", p);
        var result = Assert.Single(results);
        Assert.Equal("INSERT INTO data (field_char5) VALUES ($1)", result.FinalCommandText);
        Assert.Equal(Args(("$1", "foo")), result.Parameters);
    }

    [Theory]
    [InlineData(@"SELECT to_tsvector('fat cats ate rats') @@ to_tsquery('cat & rat')")]
    [InlineData(@"SELECT 'cat'::tsquery @> 'cat & rat'::tsquery")]
    [InlineData(@"SELECT 'cat'::tsquery <@ 'cat & rat'::tsquery")]
    [InlineData(@"SELECT 'b''la'")]
    [InlineData(@"SELECT 'type(''m.response'')#''O''%'")]
    [InlineData(@"SELECT 'abc'':str''a:str'")]
    [InlineData(@"SELECT 1 FROM "":str""")]
    [InlineData(@"SELECT 1 FROM 'yo'::str")]
    [InlineData("SELECT $\u00ffabc0$literal string :str :int$\u00ffabc0 $\u00ffabc0$")]
    [InlineData("SELECT $$:str$$")]
    public void Untouched(string sql)
    {
        var results = ParseCommand(sql, (":param", "foo"));
        var result = Assert.Single(results);
        Assert.Equal(sql, result.FinalCommandText);
        Assert.Empty(result.Parameters);
    }

    [Theory]
    [InlineData(@"SELECT 1<:param")]
    [InlineData(@"SELECT 1>:param")]
    [InlineData(@"SELECT 1<>:param")]
    [InlineData("SELECT--comment\r:param")]
    public void Parameter_gets_bound(string sql)
    {
        var p = (":param", "foo");
        var results = ParseCommand(sql, p);
        var result = Assert.Single(results);
        Assert.Equal(Args(("$1", "foo")), result.Parameters);
    }

    [Fact] //, IssueLink("https://github.com/npgsql/npgsql/issues/1177")]
    public void Parameter_gets_bound_non_ascii()
    {
        var p = ("漢字", "foo");
        var results = Assert.Single(ParseCommand("SELECT @漢字", p));
        Assert.Equal("SELECT $1", results.FinalCommandText);
        Assert.Equal(Args(("$1", "foo")), results.Parameters);
    }

    //    [Test]
    //    [TestCase(@"SELECT e'ab\'c:param'", TestName = "Estring")]
    //    [TestCase(@"SELECT/*/* -- nested comment :int /*/* *//*/ **/*/*/*/1")]
    //    [TestCase(@"SELECT 1,
    //-- Comment, @param and also :param
    //2", TestName = "LineComment")]
    //    public void Parameter_does_not_get_bound(string sql)
    //    {
    //        var p = new NpgsqlParameter(":param", "foo");
    //        var results = ParseCommand(sql, p);
    //        Assert.That(results.Single().PositionalParameters, Is.Empty);
    //    }

    //    [Test]
    //    public void Non_conforming_string()
    //    {
    //        var result = ParseCommand(@"SELECT 'abc\':str''a:str'").Single();
    //        Assert.That(result.FinalCommandText, Is.EqualTo(@"SELECT 'abc\':str''a:str'"));
    //        Assert.That(result.PositionalParameters, Is.Empty);
    //    }

    //    [Test]
    //    public void Multiquery_with_parameters()
    //    {
    //        var parameters = new NpgsqlParameter[]
    //        {
    //            new("p1", DbType.String),
    //            new("p2", DbType.String),
    //            new("p3", DbType.String),
    //        };

    //        var results = ParseCommand("SELECT @p3, @p1; SELECT @p2, @p3", parameters);

    //        Assert.That(results, Has.Count.EqualTo(2));
    //        Assert.That(results[0].FinalCommandText, Is.EqualTo("SELECT $1, $2"));
    //        Assert.That(results[0].PositionalParameters[0], Is.SameAs(parameters[2]));
    //        Assert.That(results[0].PositionalParameters[1], Is.SameAs(parameters[0]));
    //        Assert.That(results[1].FinalCommandText, Is.EqualTo("SELECT $1, $2"));
    //        Assert.That(results[1].PositionalParameters[0], Is.SameAs(parameters[1]));
    //        Assert.That(results[1].PositionalParameters[1], Is.SameAs(parameters[2]));
    //    }

    //    [Fact]
    //    public void No_output_parameters()
    //    {
    //        var p = new NpgsqlParameter("p", DbType.String) { Direction = ParameterDirection.Output };
    //        Assert.That(() => ParseCommand("SELECT @p", p), Throws.Exception);
    //    }

    //    [Fact]
    //    public void Missing_parameter_is_ignored()
    //    {
    //        var results = ParseCommand("SELECT @p; SELECT 1");
    //        Assert.That(results[0].FinalCommandText, Is.EqualTo("SELECT @p"));
    //        Assert.That(results[1].FinalCommandText, Is.EqualTo("SELECT 1"));
    //        Assert.That(results[0].PositionalParameters, Is.Empty);
    //        Assert.That(results[1].PositionalParameters, Is.Empty);
    //    }

    //    [Fact]
    //    public void Consecutive_semicolons()
    //    {
    //        var results = ParseCommand(";;SELECT 1");
    //        Assert.That(results, Has.Count.EqualTo(3));
    //        Assert.That(results[0].FinalCommandText, Is.Empty);
    //        Assert.That(results[1].FinalCommandText, Is.Empty);
    //        Assert.That(results[2].FinalCommandText, Is.EqualTo("SELECT 1"));
    //    }

    //    [Test]
    //    public void Trailing_semicolon()
    //    {
    //        var results = ParseCommand("SELECT 1;");
    //        Assert.That(results, Has.Count.EqualTo(1));
    //        Assert.That(results[0].FinalCommandText, Is.EqualTo("SELECT 1"));
    //    }

    //    [Fact]
    //    public void Empty()
    //    {
    //        var results = ParseCommand("");
    //        Assert.That(results, Has.Count.EqualTo(1));
    //        Assert.That(results[0].FinalCommandText, Is.Empty);
    //    }

    //    [Fact]
    //    public void Semicolon_in_parentheses()
    //    {
    //        var results = ParseCommand("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1; SELECT 1)");
    //        Assert.That(results, Has.Count.EqualTo(1));
    //        Assert.That(results[0].FinalCommandText, Is.EqualTo("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1; SELECT 1)"));
    //    }

    //    [Fact]
    //    public void Semicolon_after_parentheses()
    //    {
    //        var results = ParseCommand("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1); SELECT 1");
    //        Assert.That(results, Has.Count.EqualTo(2));
    //        Assert.That(results[0].FinalCommandText, Is.EqualTo("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1)"));
    //        Assert.That(results[1].FinalCommandText, Is.EqualTo("SELECT 1"));
    //    }

    //    [Fact]
    //    public void Reduce_number_of_statements()
    //    {
    //        var parser = new SqlQueryParser();

    //        var cmd = new NpgsqlCommand("SELECT 1; SELECT 2");
    //        parser.ParseRawQuery(cmd);
    //        Assert.That(cmd.InternalBatchCommands, Has.Count.EqualTo(2));

    //        cmd.CommandText = "SELECT 1";
    //        parser.ParseRawQuery(cmd);
    //        Assert.That(cmd.InternalBatchCommands, Has.Count.EqualTo(1));
    //    }

    //#if TODO
    //    [Test]
    //    public void Trim_whitespace()
    //    {
    //        _parser.ParseRawQuery("   SELECT 1\t", _params, _queries, standardConformingStrings: true);
    //        Assert.That(_queries.Single().Sql, Is.EqualTo("SELECT 1"));
    //    }
    //#endif

    #region Setup / Teardown / Utils

    public class ParseResult
    {
        public string FinalCommandText { get; set; } = "";
        public (string name, object value)[] Parameters { get; set; } = [];
        internal OrdinalResult Result { get; set; }
    }

    List<ParseResult> ParseCommand(string sql, params (string name, object value)[] parameters)
    {
        var parsed = GeneralSqlParser.Parse(sql, SqlAnalysis.SqlSyntax.PostgreSql, false);
        return parsed.ConvertAll(cmd =>
        {
            var result = cmd.TryMakeOrdinal(parameters, p => p.name,
                (p, i) => (CommandBatch.OrdinalNaming(p.name, i), p.value), out var args, out var sql);
            return new ParseResult
            {
                FinalCommandText = sql,
                Parameters = args.ToArray(),
                Result = result
            };
        });
    }

    #endregion
}
using Dapper.Internal.SqlParsing;
using System.Collections.Generic;
using System.Data;
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

    [Theory]
    [InlineData(@"SELECT e'ab\'c:param'")]
    [InlineData(@"SELECT/*/* -- nested comment :int /*/* *//*/ **/*/*/*/1")]
    [InlineData(@"SELECT 1,
    -- Comment, @param and also :param
    2")]
    public void Parameter_does_not_get_bound(string sql)
    {
        var p = (":param", "foo");
        var results = Assert.Single(ParseCommand(sql, p));
        Assert.Equal(sql, results.FinalCommandText);
        Assert.Empty(results.Parameters);
    }

    [Fact]
    public void Non_conforming_string()
    {
        var result = ParseCommand(@"SELECT 'abc\':str''a:str'").Single();
        Assert.Equal(@"SELECT 'abc\':str''a:str'", result.FinalCommandText);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void Multiquery_with_parameters()
    {
        var parameters = Args(("p1", "abc"), ("p2", "def"), ("p3", "ghi"));

        var results = ParseCommand("SELECT @p3, @p1; SELECT @p2, @p3", parameters);

        Assert.Equal(2, results.Count);

        var result = results[0];
        Assert.Equal("SELECT $1, $2", result.FinalCommandText);
        Assert.Equal(Args(("$1", "ghi"), ("$2", "abc")), result.Parameters);

        result = results[1];
        Assert.Equal("SELECT $1, $2", result.FinalCommandText);
        Assert.Equal(Args(("$1", "def"), ("$2", "ghi")), result.Parameters);
    }

    // N/A in this context of Dapper, although we should investigate further
    //[Fact]
    //public void No_output_parameters()
    //{
    //    var p = new NpgsqlParameter("p", DbType.String) { Direction = ParameterDirection.Output };
    //    Assert.That(() => ParseCommand("SELECT @p", p), Throws.Exception);
    //}

    [Fact]
    public void Missing_parameter_is_ignored()
    {
        var results = ParseCommand("SELECT @p; SELECT 1");
        Assert.Equal(2, results.Count);
        Assert.Equal("SELECT @p", results[0].FinalCommandText);
        Assert.Equal("SELECT 1", results[1].FinalCommandText);
        Assert.Empty(results[0].Parameters);
        Assert.Empty(results[1].Parameters);
    }

    [Fact]
    public void Consecutive_semicolons()
    {
        var results = ParseCommand(";;SELECT 1");
        Assert.Equal(3, results.Count);
        Assert.Equal("", results[0].FinalCommandText);
        Assert.Equal("", results[1].FinalCommandText);
        Assert.Equal("SELECT 1", results[0].FinalCommandText);

        // what actually happens (passes)
        // Assert.Equal("SELECT 1", Assert.Single(results).FinalCommandText);
    }

    [Fact]
    public void Trailing_semicolon()
    {
        var results = Assert.Single(ParseCommand("SELECT 1;"));
        Assert.Equal("SELECT 1", results.FinalCommandText);
    }

    [Fact]
    public void Empty()
    {
        var results = ParseCommand("");
        Assert.Equal("", Assert.Single(results).FinalCommandText);
    }

    [Fact]
    public void Semicolon_in_parentheses()
    {
        var results = Assert.Single(ParseCommand("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1; SELECT 1)"));
        Assert.Equal("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1; SELECT 1)", results.FinalCommandText);
    }

    [Fact]
    public void Semicolon_after_parentheses()
    {
        var results = ParseCommand("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1); SELECT 1");
        Assert.Equal(2, results.Count);
        Assert.Equal("CREATE OR REPLACE RULE test AS ON UPDATE TO test DO (SELECT 1)", results[0].FinalCommandText);
        Assert.Equal("SELECT 1", results[1].FinalCommandText);
    }

    [Fact]
    public void Reduce_number_of_statements()
    {
        Assert.Equal(2, ParseCommand("SELECT 1; SELECT 2").Count);
        Assert.Single(ParseCommand("SELECT 1"));
    }

    [Fact]
    public void Trim_whitespace()
    {
        var result = Assert.Single(ParseCommand("   SELECT 1\t", strip: true));
        Assert.Equal("SELECT 1", result.FinalCommandText);
        Assert.Empty(result.Parameters);
    }

    #region Setup / Teardown / Utils

    public class ParseResult
    {
        public string FinalCommandText { get; set; } = "";
        public (string name, object value)[] Parameters { get; set; } = [];
        internal OrdinalResult Result { get; set; }
    }

    List<ParseResult> ParseCommand(string sql, params (string name, object value)[] parameters)
        => ParseCommand(sql, false, parameters);
    List<ParseResult> ParseCommand(string sql, bool strip, params (string name, object value)[] parameters)
    {
        var parsed = GeneralSqlParser.Parse(sql, SqlAnalysis.SqlSyntax.PostgreSql, strip);
        if (parsed.Count == 0)
        {
            return new List<ParseResult>
            {
                new ParseResult
                {
                    FinalCommandText = "",
                    Parameters = [],
                    Result = sql == "" ? OrdinalResult.NoChange : OrdinalResult.Success,
                }
            };
        }
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
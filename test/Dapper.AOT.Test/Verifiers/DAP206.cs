using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP206 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValidSql() => SqlVerifyAsync("exec valid");
    
    [Fact]
    public Task ParseError() => SqlVerifyAsync("{|#0:|}111 invalid",
        Diagnostic(Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near '111'."));

    // SQL handed to a non-Dapper method's "sql" parameter is parsed without its arguments, so
    // nothing says whether @ids is a list; "in @ids" is only ever valid as list expansion
    [Fact]
    public Task ListExpansionViaSqlParameter() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        class SomeCode
        {
            static void Run(string sql, object args) { }
            public void Usage()
            {
                Run("select Id from Customers where Id in @ids", null);
                Run("select Id from Customers where Id NOT IN @ids and Region = @region", null);
                Run(@"select Id from Customers
                      where Id in
                            @ids", null);
                Run("{|#0:|}111 invalid where Id in @ids", null);
                Run("select Id from Customers where Id in @ids and Region = {|#1:|};", null);
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near '111'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(1).WithArguments(46010, "Incorrect syntax near ';'."),
        ]);

    // the brackets are paid for with spaces or tabs beside @x, never with a line break, so an
    // error after the list is reported where it is
    [Fact]
    public Task ListExpansionKeepsLaterLocations() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        class SomeCode
        {
            static void Run(string sql, object args) { }
            public void Usage()
            {
                Run("select Id from Customers where Id in\t@ids\tand Region = {|#0:|};", null);
                Run("select Id from Customers where Id in@ids  and Region = {|#3:|};", null);
                Run("select Id from Customers where Email = 'in@corp.com' and Region = {|#4:|};", null);
                Run("select Id /* sign in@home */ from Customers where Region = {|#5:|};", null);
                Run(@"select Id from Customers where Id in
        @ids  and Region = {|#1:|};", null);
                Run(@"select Id from Customers
                      where Id in @ids
                      and Region = {|#2:|};", null);
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(1).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(2).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(3).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(4).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(5).WithArguments(46010, "Incorrect syntax near ';'."),
        ]);

    // VB strings map locations by line and column too
    [Fact]
    public Task VBListExpansionViaSqlParameter() => VBVerifyAsync("""
        Class SomeCode
            Shared Sub Run(sql As String, args As Object)
            End Sub
            Public Sub Usage()
                Run("select Id from Customers where Id in @ids and Region = {|#0:|};", Nothing)
                Run("select Id from Customers where Id in
        @ids  and Region = {|#1:|};", Nothing)
            End Sub
        End Class
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(1).WithArguments(46010, "Incorrect syntax near ';'."),
        ]);

    [Fact]
    public Task ListExpansionViaSqlProperty() => CSVerifyAsync("""
        using Dapper;

        class SomeCode
        {
            [Sql]
            public string Text { get; set; }
            public void Usage()
            {
                Text = "select Id from Customers where Id in @ids and Region = {|#0:|};";
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near ';'.")]);

    [Fact]
    public Task ListExpansionWithDynamicParameters() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        [DapperAot(false)]
        class SomeCode
        {
            public void Foo(DbConnection conn, DynamicParameters args)
            {
                conn.Execute("select Id from Customers where Id in @ids and Region = {|#0:|};", args);
            }
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near ';'.")]);

    // arguments typed as object hide their members too; the error after the list proves the SQL was parsed
    [Fact]
    public Task ListExpansionWithUntypedArguments() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn, object args)
            {
                conn.Execute("select Id from Customers where Id in @ids and Region = {|#0:|};", {|#1:args|});
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near ';'."),
            Diagnostic(Diagnostics.UntypedParameter).WithLocation(1),
            Diagnostic(Diagnostics.SqlParametersNotDetected).WithLocation(1),
        ]);

    // nothing will expand @ids here: no arguments at all, a command's own text, and a library's
    // own "sql" parameter (as EF Core's FromSqlRaw) all run the SQL as written
    [Fact]
    public Task InListWithNothingToExpandIsReported() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;
        using Microsoft.Data.SqlClient;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn, DbCommand cmd, SqlConnection sqlConn)
            {
                conn.Execute("select Id from Customers where Id in {|#0:|}@ids");
                cmd.CommandText = "select Id from Customers where Id in {|#1:|}@ids";
                using var sqlCmd = new SqlCommand("select Id from Customers where Id in {|#2:|}@ids", sqlConn);
                _ = DapperAotExtensions.GetCommandType("select Id from Customers where Id in {|#3:|}@ids");
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near '@ids'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(1).WithArguments(46010, "Incorrect syntax near '@ids'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(2).WithArguments(46010, "Incorrect syntax near '@ids'."),
            Diagnostic(Diagnostics.ParseError).WithLocation(3).WithArguments(46010, "Incorrect syntax near '@ids'."),
        ]);

    // with the arguments in view, a scalar after "in" is still the error it will be at runtime
    [Fact]
    public Task InScalarWithKnownParametersIsReported() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                conn.Execute({|#1:"select Id from Customers where Id in {|#0:|}@id"|}, new { id = 1 });
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near '@id'."),
            Diagnostic(Diagnostics.UnusedParameter).WithLocation(1).WithArguments("id"),
        ]);

    // Dapper maps byte[] to DbType.Binary before it considers IEnumerable, so a byte[] member is
    // one varbinary value, never an expanded list; sbyte[] has no such mapping and does expand
    [Fact]
    public Task ByteArrayIsNotListExpanded() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn, byte[] photo, sbyte[] codes, int[] ids)
            {
                conn.Execute("exec dbo.SavePhoto @id, @photo", new { id = 1, photo });
                conn.Execute("update Customers set Photo = @photo where Id = @id", new { id = 1, photo });
                conn.Execute({|#2:"exec dbo.SaveCodes @id, {|#0:|}@codes"|}, new { id = 1, codes });
                conn.Execute({|#3:"exec dbo.SaveIds @id, {|#1:|}@ids"|}, new { id = 1, ids });
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ParseError).WithLocation(0).WithArguments(46010, "Incorrect syntax near '('."),
            Diagnostic(Diagnostics.UnusedParameter).WithLocation(2).WithArguments("id"),
            Diagnostic(Diagnostics.UnusedParameter).WithLocation(2).WithArguments("codes"),
            Diagnostic(Diagnostics.ParseError).WithLocation(1).WithArguments(46010, "Incorrect syntax near '('."),
            Diagnostic(Diagnostics.UnusedParameter).WithLocation(3).WithArguments("id"),
            Diagnostic(Diagnostics.UnusedParameter).WithLocation(3).WithArguments("ids"),
        ]);
}
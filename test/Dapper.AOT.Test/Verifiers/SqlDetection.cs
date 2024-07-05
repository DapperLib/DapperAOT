using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class SqlDetection : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task CSViaDapper() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                conn.Execute("exec valid");
                conn.Execute("{|#0:|}111 invalid");
            }
        }
        """, DefaultConfig, [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111.")]);

    [Fact]
    public Task CSVerifyQuestionMarkInQuery_LikePseudoPositional() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
    
        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<int>("select {|#0:'this ?looks? like pseudo-positional'|}");                                                                     // line 9
                _ = conn.Query<int>("select 'this ? does not look ? like pseudo-positional because of spaces'");                                                // line 10
                _ = conn.Query<int>("select * from Orders where Id = ?id?", new Poco { Id = "1" });                                                             // line 11
                _ = conn.Query<int>("select * from Orders where Id = ?id? and Name = ?name?", new Poco { Id = "1", Name = "me" });                              // line 12
                _ = conn.Query<int>("select 'this ?' + 'does not look like ? pseudo-positional' + 'because only 1 question mark is in every string part ?'");   // line 13
                _ = conn.Query<int>(@"                                                                                                                          // line 14
                    SELECT *                                                                                                                                    // line 15
                    FROM Orders                                                                                                                                 // line 16
                    WHERE Id = ?id?                                                                                                                             // line 17
                      AND Name = ?name?",                                                                                                                       // line 18
                    new Poco { Id = "1", Name = "me" });                                                                                                        // line 19
            }
        }

        class Poco
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    """, DefaultConfig, [
        Diagnostic(DapperAnalyzer.Diagnostics.PseudoPositionalParameter).WithLocation(0)
    ]);

    [Fact]
    public Task CSViaProperty() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        class SomeCode
        {
            public void Foo(DbCommand cmd)
            {
                cmd.CommandText = "exec valid";
                cmd.CommandText = "{|#0:|}111 invalid";

                CommandText = "222 invalid";
                Bar = "333 invalid";
                Blap = "{|#1:|}444 invalid";
            }

            public string CommandText {get;set;}
            public string Bar {get;set;}
            [Sql]
            public string Blap {get;set;}
        }
        """, DefaultConfig, [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(1).WithArguments(46010, "Incorrect syntax near 444.")]);

    [Fact]
    public Task CSViaParam() => CSVerifyAsync("""
        using System.Data.Common;
        using Dapper;

        class SomeCode
        {
            static void Foo(string sql) {}
            static void Bar(string whatever) {}
            static void Blap([Sql] string whatever) {}
            public void Usage()
            {
                Foo("exec valid");
                Foo("{|#0:|}111 invalid");
                Bar("222 invalid");
                Blap("{|#1:|}333 invalid");
            }
        }
        """, DefaultConfig,
        [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0)
            .WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1)
            .WithArguments(46010, "Incorrect syntax near 333.")]);

    [Fact]
    public Task VBViaDapper() => VBVerifyAsync("""
        Imports Dapper
        Imports System.Data.Common

        <DapperAot(True)>
        Class SomeCode
            Public Sub Foo(conn As DbConnection)
                conn.Execute("exec valid")
                conn.Execute("{|#0:|}111 invalid")
            End Sub
        End Class
        """, DefaultConfig, [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111.")]);

    [Fact]
    public Task VBViaProperty() => VBVerifyAsync("""
        Imports System.Data.Common
        Imports Dapper

        Class SomeCode
            Public Sub Foo(cmd as DbCommand)
                cmd.CommandText = "exec valid"
                cmd.CommandText = "{|#0:|}111 invalid"

                CommandText = "222 invalid"
                Bar = "333 invalid"
                Blap = "{|#1:|}444 invalid"
            End Sub

            Public Property CommandText As String
            Public Property Bar As String
            <Sql>
            Public Property Blap As String
        End Class
        """, DefaultConfig, [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
    Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(1).WithArguments(46010, "Incorrect syntax near 444.")]);

    [Fact]
    public Task VBViaParam() => VBVerifyAsync("""
        Imports System.Data.Common
        Imports Dapper

        Class SomeCode
            Sub Foo(sql as String)
            End Sub
            Sub Bar(whatever as String)
            End Sub
            Sub Blap(<Sql> whatever As String)
            End Sub
            Public Sub Usage()
                Foo("exec valid")
                Foo("{|#0:|}111 invalid")
                Bar("222 invalid")
                Blap("{|#1:|}333 invalid")
            End Sub
        End Class
        """, DefaultConfig,
        [Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0)
            .WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1)
            .WithArguments(46010, "Incorrect syntax near 333.")]);

    [Fact]
    public Task CSharpSmokeTestVanilla() => CSVerifyAsync(""""
        using Microsoft.Data.SqlClient;
        using Dapper;
        static class Program
        {
            static void Main()
            {
        using var conn = new SqlConnection("my connection string here");
        string name = "abc";
        conn.{|#0:Execute|}("""
            select Id, Name, Age
            from Users
            where Id = @id
            and Name = @name
            and Age = @age
            and Something = {|#1:null|}
            """, new
        {
            name,
            id = 42,
            age = 24,
        });

        using var cmd = new SqlCommand("should ' verify this too", conn);
        cmd.CommandText = """
            select Id, Name, Age
            from Users
            where Id = @id
            and Name = @name
            and Age = @age
            and Something = {|#2:null|}
            """;
        cmd.ExecuteNonQuery();
            }
        }
"""", [], [
        // (not enabled) Diagnostic(DapperAnalyzer.Diagnostics.DapperAotNotEnabled).WithLocation(0).WithArguments(1),
        Diagnostic(DapperAnalyzer.Diagnostics.ExecuteCommandWithQuery).WithLocation(0),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(1),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(2),
    ], SqlSyntax.General, refDapperAot: false);

    [Fact]
    public Task VBSmokeTestVanilla() => VBVerifyAsync("""
        Imports Dapper
        Imports Microsoft.Data.SqlClient
        Module Program
            Sub Main()
                Using conn As New SqlConnection("my connection string here")
                    Dim name As String = "name"
                    conn.{|#0:Execute|}("
            select Id, Name, Age
            from Users
            where Id = @id
            and Name = @name
            and Age = @age
            and Something = {|#1:null|}", New With {name, .id = 42, .age = 24 })

                    Using cmd As New SqlCommand("should ' verify this too", conn)
                        cmd.CommandText = "
            select Id, Name, Age
            from Users
            where Id = @id
            and Name = @name
            and Age = @age
            and Something = {|#2:null|}"
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
            End Sub
        End Module
""", [], [
        Diagnostic(DapperAnalyzer.Diagnostics.ExecuteCommandWithQuery).WithLocation(0),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(1),
        Diagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison).WithLocation(2)
    ], SqlSyntax.General, refDapperAot: false);
}
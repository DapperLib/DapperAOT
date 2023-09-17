using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP206 : Verifier<DapperAnalyzer>
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
""", Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."));

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
""", Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(1).WithArguments(46010, "Incorrect syntax near 444."));

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
""",
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0)
            .WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1)
            .WithArguments(46010, "Incorrect syntax near 333."));

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
""", Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
    .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."));

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
""", Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."),
    Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
.WithLocation(1).WithArguments(46010, "Incorrect syntax near 444."));

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
""",
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0)
            .WithArguments(46010, "Incorrect syntax near 111."),
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(1)
            .WithArguments(46010, "Incorrect syntax near 333."));
}
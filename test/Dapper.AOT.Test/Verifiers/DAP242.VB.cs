using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public partial class DAP242 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task VBConcatenatedStringDetection_DirectUsage_PlusInt() => VerifyVBInterpolatedTest("""
        Dim id As Integer = 42
        conn.Execute({|#0:"select Id from Customers where Id = " + id|})
    """);

    [Fact]
    public Task VBConcatenatedStringDetection_DirectUsage_PlusString() => VerifyVBInterpolatedTest("""
        Dim id = "42"
        conn.Execute({|#0:"select Id from Customers where Id = " + id|})
    """);

    [Fact]
    public Task VBConcatenatedStringDetection_DirectUsage_MultiVariablesConcat() => VerifyVBInterpolatedTest("""
        Dim id = 42
        Dim name = "dima"
        conn.Execute({|#0:"select Id from Customers where Id = " + id + " and name = " + name|})
    """);

    [Fact]
    public Task VBConcatenatedStringDetection_LocalVariableUsage() => VerifyVBInterpolatedTest("""
        Dim id As Integer = 42
        Dim sqlQuery As String = "select Id from Customers where Id = " + id
        conn.Execute({|#0:sqlQuery|})
    """);

    private Task VerifyVBInterpolatedTest(string methodCode) => VBVerifyAsync($$"""
        Imports Dapper
        Imports System.Data.Common
    
        <DapperAot(True)>
        Class SomeCode
            Public Sub Foo(conn As DbConnection)
                {{methodCode}}
            End Sub
        End Class
    """, DefaultConfig, [Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0)]);
}
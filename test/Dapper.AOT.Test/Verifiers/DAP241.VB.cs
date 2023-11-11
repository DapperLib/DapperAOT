using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public partial class DAP241
{
    [Fact]
    public Task VBInterpolatedStringDetection_DirectUsage() => VerifyVBInterpolatedTest("""
        Dim id As Integer = 42
        conn.Execute({|#0:$"select Id from Customers where Id = {id}"|})
    """);

    [Fact]
    public Task VBInterpolatedStringDetection_LocalVariableUsage() => VerifyVBInterpolatedTest("""
        Dim id As Integer = 42
        Dim sqlQuery As String = $"select Id from Customers where Id = {id}"
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
    """, DefaultConfig, [Diagnostic(Diagnostics.InterpolatedStringSqlExpression).WithLocation(0)]);
}
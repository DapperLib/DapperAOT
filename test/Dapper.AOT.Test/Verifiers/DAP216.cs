using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP216 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InsertColumnsNotSpecified() => SqlVerifyAsync("""
        {|#0:insert SomeTable values (42)|}
        insert SomeTable (A) values (42)
        """, Diagnostic(Diagnostics.InsertColumnsNotSpecified).WithLocation(0));
    
}
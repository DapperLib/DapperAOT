using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP225 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task FromMultiTableMissingAlias() => SqlVerifyAsync("""
        select a.Id
        from A a
        inner join {|#0:B|} on a.X = a.Id

        select a.Id
        from A a
        inner join B b on b.X = a.Id
        """, Diagnostic(Diagnostics.FromMultiTableMissingAlias).WithLocation(0));
    
}
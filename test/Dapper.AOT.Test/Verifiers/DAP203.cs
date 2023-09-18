using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP203 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InsertWithGlobalIdentity() => SqlVerifyAsync("""
        insert SomeTable (A) values (1)
        select {|#0:@@identity|}
        """,
        Diagnostic(DapperAnalyzer.Diagnostics.GlobalIdentity)
            .WithLocation(0));
}
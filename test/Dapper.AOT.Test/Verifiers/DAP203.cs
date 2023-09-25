using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP203 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task GlobalIdentity() => SqlVerifyAsync("""
        declare @identity int = 42;
        insert SomeTable (A) values (1)

        select {|#0:@@identity|}

        select @identity
        """,
        Diagnostic(Diagnostics.GlobalIdentity)
            .WithLocation(0));
}
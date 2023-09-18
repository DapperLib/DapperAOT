using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP204 : Verifier<DapperAnalyzer>
{

    [Fact]
    public Task SelectScopeIdentity() => SqlVerifyAsync("""
        insert SomeTable (A) values (1)
        select {|#0:scope_identity()|}
        """,
        Diagnostic(Diagnostics.SelectScopeIdentity)
            .WithLocation(0));

    [Fact]
    public Task InsertWithOutput() => SqlVerifyAsync("""
        insert SomeTable (A)
        output inserted.Id
        values (1)
        """);
}
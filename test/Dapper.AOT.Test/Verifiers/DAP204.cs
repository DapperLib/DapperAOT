using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP204 : Verifier<DapperAnalyzer>
{

    [Fact]
    public Task InsertWithScopeIdentity() => SqlVerifyAsync("""
        insert SomeTable (A) values (1)
        select {|#0:scope_identity()|}
        """,
        Diagnostic(DapperAnalyzer.Diagnostics.SelectScopeIdentity)
            .WithLocation(0));

    [Fact]
    public Task InsertWithOutput() => SqlVerifyAsync("""
        insert SomeTable (A)
        output inserted.Id
        values (1)
        """);
}
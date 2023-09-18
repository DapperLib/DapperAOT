using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP202 : Verifier<DapperAnalyzer>
{

    [Fact]
    public Task DuplicateVariableDeclaration() => SqlVerifyAsync("""
        declare @a int = 1;
        declare {|#0:@a|} int;
        select @a;
        """,
        Diagnostic(Diagnostics.DuplicateVariableDeclaration)
            .WithLocation(0).WithArguments("@a"));

    [Fact]
    public Task SingleLocal() => SqlVerifyAsync("""
        declare @a int = 1;
        select @a;
        """);
}
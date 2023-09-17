using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP202 : Verifier<DapperAnalyzer>
{

    [Fact]
    public Task DuplicateLocal() => SqlVerifyAsync("""
        declare @a int = 1;
        declare {|#0:@a|} int;
        select @a;
        """,
        Diagnostic(DapperAnalyzer.Diagnostics.DuplicateVariableDeclaration)
            .WithLocation(0).WithArguments("@a"));

    [Fact]
    public Task SingleLocal() => SqlVerifyAsync("""
        declare @a int = 1;
        select @a;
        """);
}
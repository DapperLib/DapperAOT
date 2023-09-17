using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP206 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValidSql() => SqlVerifyAsync("exec valid");
    
    [Fact]
    public Task InvalidSql() => SqlVerifyAsync("{|#0:|}111 invalid",
        Diagnostic(DapperAnalyzer.Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."));
}
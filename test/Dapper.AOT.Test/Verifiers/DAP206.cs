using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP206 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValidSql() => SqlVerifyAsync("exec valid");
    
    [Fact]
    public Task ParseError() => SqlVerifyAsync("{|#0:|}111 invalid",
        Diagnostic(Diagnostics.ParseError)
            .WithLocation(0).WithArguments(46010, "Incorrect syntax near 111."));
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public class DAP201 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task MultipleBatches() => SqlVerifyAsync("""
        print 'abc'
        go
        {|#0:print 'def'|}
        """,
    Diagnostic(Diagnostics.MultipleBatches)
        .WithLocation(0));

    // note: a command of JUST "GO", or *ending* with "GO" is really hard to isolate; I'm not going to
    // worry about it - at that point, it is just self-inflicted, and just worry about true multi-batch commands

    [Fact]
    public Task SingleBatch() => SqlVerifyAsync("""
        print 'abc'
        """);
}
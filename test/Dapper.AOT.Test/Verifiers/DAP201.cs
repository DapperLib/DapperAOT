using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Verifiers;

public class DAP201 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task MultipleBatches() => SqlVerifyAsync("""
        print 'abc'
        go
        {|#0:print 'def'|}
        """,
    Diagnostic(DapperAnalyzer.Diagnostics.MultipleBatches)
        .WithLocation(0));

    [Fact]
    public Task EndingWithGo() => SqlVerifyAsync("""
        print 'abc'
        go{|#0:|}
        """,
        Diagnostic(DapperAnalyzer.Diagnostics.MultipleBatches)
            .WithLocation(0));

    [Fact]
    public Task SingleBatch() => SqlVerifyAsync("""
        print 'abc'
        """);
}
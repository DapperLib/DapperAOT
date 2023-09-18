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

    [Fact]
    public Task MultipleBatches_EndingWithGo() => SqlVerifyAsync("""
        print 'abc'
        go{|#0:|}
        """,
        Diagnostic(Diagnostics.MultipleBatches)
            .WithLocation(0));

    [Fact]
    public Task SingleBatch() => SqlVerifyAsync("""
        print 'abc'
        """);
}
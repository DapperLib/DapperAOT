using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// credit: https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/

namespace Dapper.AOT.Test;

public static class AnalyzerVerifier<TAnalyzer>
   where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return CSharpAnalyzerVerifier<TAnalyzer, XUnitVerifier>.Diagnostic(diagnosticId);
    }

    public static async Task VerifyAnalyzerAsync(
       string source,
       params DiagnosticResult[] expected)
    {
        var test = new AnalyzerTest(source, expected);
        await test.RunAsync(CancellationToken.None);
    }

    private class AnalyzerTest : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
    {
        public AnalyzerTest(
           string source,
           params DiagnosticResult[] expected)
        {
            TestCode = source;
            ExpectedDiagnostics.AddRange(expected);
#if NETFRAMEWORK
            ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default;
#else
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#endif

            TestState.AdditionalReferences.Add(typeof(SqlMapper).Assembly);
            TestState.AdditionalReferences.Add(typeof(DapperAotAttribute).Assembly);
        }
    }
}
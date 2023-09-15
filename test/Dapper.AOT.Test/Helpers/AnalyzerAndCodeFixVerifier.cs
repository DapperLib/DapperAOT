using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// credit: https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/

namespace Dapper.AOT.Test;

public static class AnalyzerAndCodeFixVerifier<TAnalyzer, TCodeFix>
   where TAnalyzer : DiagnosticAnalyzer, new()
   where TCodeFix : CodeFixProvider, new()
{
    public static DiagnosticResult Diagnostic(string diagnosticId)
    {
        return CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>
                  .Diagnostic(diagnosticId);
    }

    public static async Task VerifyCodeFixAsync(
       string source,
       string fixedSource,
       params DiagnosticResult[] expected)
    {
        var test = new CodeFixTest(source, fixedSource, expected);
        await test.RunAsync(CancellationToken.None);
    }

    private class CodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
    {
        public CodeFixTest(
           string source,
           string fixedSource,
           params DiagnosticResult[] expected)
        {
            TestCode = source;
            FixedCode = fixedSource;
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
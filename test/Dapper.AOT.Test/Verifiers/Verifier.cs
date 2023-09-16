using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.AOT.Test.Verifiers;
// inspiration: https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/
public abstract class Verifier
{
    public CancellationToken CancellationToken { get; private set; }

    protected static DiagnosticResult Diagnostic(DiagnosticDescriptor diagnostic)
        => new DiagnosticResult(diagnostic);
    protected static DiagnosticResult InterceptorsNotEnabled = Diagnostic(CodeAnalysis.DapperInterceptorGenerator.Diagnostics.InterceptorsNotEnabled);

    public Task VerifyAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, expected);
    }
    public Task VerifyAsync<TAnalyzer, TCodeFixProvider>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, expected);
    }

    protected Task ExecuteAsync(AnalyzerTest<XUnitVerifier> test, string source, DiagnosticResult[] expected)
    {
        test.TestCode = source;
        test.ExpectedDiagnostics.AddRange(expected);
#if NETFRAMEWORK
        test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default;
#elif NET8_0_OR_GREATER
        test.ReferenceAssemblies = new ReferenceAssemblies("net8.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
            Path.Combine("ref", "net8.0"));
#elif NET7_0_OR_GREATER
        test.ReferenceAssemblies = new ReferenceAssemblies("net7.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "7.0.0"),
            Path.Combine("ref", "net7.0"));
#else
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#endif
        test.TestState.AdditionalReferences.Add(typeof(SqlMapper).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(DapperAotAttribute).Assembly);

        return test.RunAsync(CancellationToken);
    }
}

public class Verifier<TAnalyzer> : Verifier where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => base.VerifyAsync<TAnalyzer>(source, expected);

    new protected Task VerifyAsync<TCodeFixProvider>(string source, params DiagnosticResult[] expected)
        where TCodeFixProvider : CodeFixProvider, new()
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, expected);
}
public class Verifier<TAnalyzer, TCodeFixProvider> : Verifier
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    protected Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, expected);
}

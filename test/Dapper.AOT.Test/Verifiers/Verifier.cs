using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System;
using System.Collections.Generic;
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

    protected static DiagnosticResult InterceptorsGenerated(int handled, int total,
        int interceptors, int commands, int readers)
        => new DiagnosticResult(CodeAnalysis.DapperInterceptorGenerator.Diagnostics.InterceptorsGenerated)
        .WithArguments(handled, total, interceptors, commands, readers);

    public Task VerifyAsync<TAnalyzer>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected);
    }
    public Task VerifyAsync<TAnalyzer, TCodeFixProvider>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFixProvider : CodeFixProvider, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected);
    }

    protected Task ExecuteAsync(AnalyzerTest<XUnitVerifier> test, string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected)
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
        if (transforms is not null) test.SolutionTransforms.AddRange(transforms);
        return test.RunAsync(CancellationToken);
    }
    protected static Func<Solution, ProjectId, Solution> InterceptorsEnabled = WithFeatures(
        new("InterceptorsPreview", "true"), // rc 1
        new("InterceptorsPreviewNamespaces", "Dapper.AOT") // rc2 ?
    );
    protected static Func<Solution, ProjectId, Solution> CSharpPreview = WithLanguageVersion(LanguageVersion.Preview);

    protected static Func<Solution, ProjectId, Solution>[] DefaultConfig = new[] { InterceptorsEnabled, CSharpPreview };

    protected static Func<Solution, ProjectId, Solution> WithLanguageVersion(LanguageVersion version)
        => WithParseOptions(options => options.WithLanguageVersion(version));
    protected static Func<Solution, ProjectId, Solution> WithFeatures(params KeyValuePair<string, string>[] features)
        => WithParseOptions(options => options.WithFeatures(features));
    protected static Func<Solution, ProjectId, Solution> WithParseOptions(Func<CSharpParseOptions, CSharpParseOptions> func)
    {
        if (func is null) return static (solution, _) => solution;
        return (solution, projectId) =>
        {
            var options = solution.GetProject(projectId)?.ParseOptions as CSharpParseOptions;
            if (options is null) return solution;
            return solution.WithProjectParseOptions(projectId, func(options));
        };
    }

}

public class Verifier<TAnalyzer> : Verifier where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected Task VerifyAsync(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        params DiagnosticResult[] expected)
        => base.VerifyAsync<TAnalyzer>(source, transforms, expected);
    protected Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => base.VerifyAsync<TAnalyzer>(source, DefaultConfig, expected);

    new protected Task VerifyAsync<TCodeFixProvider>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        params DiagnosticResult[] expected)
        where TCodeFixProvider : CodeFixProvider, new()
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, transforms, expected);

    protected Task VerifyAsync<TCodeFixProvider>(string source, params DiagnosticResult[] expected)
        where TCodeFixProvider : CodeFixProvider, new()
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, DefaultConfig, expected);
}
public class Verifier<TAnalyzer, TCodeFixProvider> : Verifier
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    protected Task VerifyAsync(string source, params DiagnosticResult[] expected)
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, DefaultConfig, expected);
    protected Task VerifyAsync(string source, Func<Solution, ProjectId, Solution>[] transforms, 
        params DiagnosticResult[] expected)
        => VerifyAsync<TAnalyzer, TCodeFixProvider>(source, transforms, expected);
}

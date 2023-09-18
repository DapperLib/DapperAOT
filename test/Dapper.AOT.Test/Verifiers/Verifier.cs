using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Dapper.SqlAnalysis.TSqlProcessor;

namespace Dapper.AOT.Test.Verifiers;
// inspiration: https://www.thinktecture.com/en/net/roslyn-source-generators-analyzers-code-fixes-testing/
public abstract class Verifier
{
    public CancellationToken CancellationToken { get; private set; }

    protected static DiagnosticResult Diagnostic(DiagnosticDescriptor diagnostic)
        => new DiagnosticResult(diagnostic);
    protected static DiagnosticResult InterceptorsNotEnabled = Diagnostic(DapperInterceptorGenerator.Diagnostics.InterceptorsNotEnabled);

    protected static DiagnosticResult InterceptorsGenerated(int handled, int total,
        int interceptors, int commands, int readers)
        => new DiagnosticResult(DapperInterceptorGenerator.Diagnostics.InterceptorsGenerated)
        .WithArguments(handled, total, interceptors, commands, readers);

    internal Task CSVerifyAsync<TAnalyzer>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax, ModeFlags debugModeFlags = ModeFlags.None)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected, sqlSyntax, debugModeFlags);
    }
    internal Task CSVerifyAsync<TAnalyzer, TCodeFix>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax, ModeFlags debugModeFlags = ModeFlags.None)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected, sqlSyntax, debugModeFlags);
    }

    internal Task VBVerifyAsync<TAnalyzer>(string source,
    Func<Solution, ProjectId, Solution>[] transforms,
    DiagnosticResult[] expected, SqlSyntax sqlSyntax)
    where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new VisualBasicAnalyzerTest<TAnalyzer, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected, sqlSyntax, ModeFlags.None);
    }
    internal Task VBVerifyAsync<TAnalyzer, TCodeFix>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new VisualBasicCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>();
        return ExecuteAsync(test, source, transforms, expected, sqlSyntax, ModeFlags.None);
    }

    internal Task ExecuteAsync(AnalyzerTest<XUnitVerifier> test, string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax, ModeFlags debugModeFlags)
    {
        test.TestCode = source;
        if (expected is not null)
        {
            test.ExpectedDiagnostics.AddRange(expected);
        }
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
        if (sqlSyntax == SqlSyntax.SqlServer && debugModeFlags == ModeFlags.None)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", AssumeSqlServer));
        }
        else if (sqlSyntax != SqlSyntax.General || debugModeFlags != ModeFlags.None)
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", CreateEditorConfig(sqlSyntax, debugModeFlags)));
        }
        test.TestState.AdditionalReferences.Add(typeof(SqlMapper).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(DapperAotAttribute).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(System.Data.SqlClient.SqlConnection).Assembly);
        test.TestState.AdditionalReferences.Add(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly);
        if (transforms is not null)
        {
            test.SolutionTransforms.AddRange(transforms);
        }
        return test.RunAsync(CancellationToken);
    }

    static SourceText CreateEditorConfig(SqlSyntax syntax, ModeFlags debugModeFlags)
    {
        var sb = new StringBuilder().AppendLine("is_global = true");
        if (syntax != SqlSyntax.General) sb.Append(GlobalOptions.Keys.GlobalOptions_DapperSqlSyntax).Append(" = ").Append(syntax).AppendLine();
        if (debugModeFlags != ModeFlags.None) sb.Append(GlobalOptions.Keys.GlobalOptions_DapperDebugModeFlags).Append(" = ").Append(debugModeFlags).AppendLine();
        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static readonly SourceText AssumeSqlServer = CreateEditorConfig(SqlSyntax.SqlServer, ModeFlags.None);

    protected static Func<Solution, ProjectId, Solution> InterceptorsEnabled = WithFeatures(
        new("InterceptorsPreview", "true"), // rc 1
        new("InterceptorsPreviewNamespaces", "Dapper.AOT") // rc2 ?
    );

    protected static Func<Solution, ProjectId, Solution> CSharpPreview = WithCSharpLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);

    protected static Func<Solution, ProjectId, Solution> VisualBasic14 = WithVisualBasicLanguageVersion(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14);

    protected static Func<Solution, ProjectId, Solution>[] DefaultConfig = new[] {
        InterceptorsEnabled, CSharpPreview, VisualBasic14 };

    protected static Func<Solution, ProjectId, Solution> WithCSharpLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion version)
        => WithCSharpParseOptions(options => options.WithLanguageVersion(version));

    protected static Func<Solution, ProjectId, Solution> WithVisualBasicLanguageVersion(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion version)
        => WithVisualBasicParseOptions(options => options.WithLanguageVersion(version));
    protected static Func<Solution, ProjectId, Solution> WithFeatures(params KeyValuePair<string, string>[] features)
        => WithParseOptions(options => options.WithFeatures(features));
    protected static Func<Solution, ProjectId, Solution> WithParseOptions(Func<ParseOptions, ParseOptions> func)
    {
        if (func is null) return static (solution, _) => solution;
        return (solution, projectId) =>
        {
            var options = solution.GetProject(projectId)?.ParseOptions;
            if (options is null) return solution;
            return solution.WithProjectParseOptions(projectId, func(options));
        };
    }
    protected static Func<Solution, ProjectId, Solution> WithCSharpParseOptions(Func<CSharpParseOptions, CSharpParseOptions> func)
    {
        if (func is null) return static (solution, _) => solution;
        return (solution, projectId) =>
        {
            var options = solution.GetProject(projectId)?.ParseOptions as CSharpParseOptions;
            if (options is null) return solution;
            return solution.WithProjectParseOptions(projectId, func(options));
        };
    }
    protected static Func<Solution, ProjectId, Solution> WithVisualBasicParseOptions(Func<VisualBasicParseOptions, VisualBasicParseOptions> func)
    {
        if (func is null) return static (solution, _) => solution;
        return (solution, projectId) =>
        {
            var options = solution.GetProject(projectId)?.ParseOptions as VisualBasicParseOptions;
            if (options is null) return solution;
            return solution.WithProjectParseOptions(projectId, func(options));
        };
    }

}

public class Verifier<TAnalyzer> : Verifier where TAnalyzer : DiagnosticAnalyzer, new()
{
    internal Task SqlVerifyAsync(string sql,
        params DiagnosticResult[] expected) => SqlVerifyAsync(sql, ModeFlags.None, expected);
    
    internal Task SqlVerifyAsync(string sql, ModeFlags debugModeFlags, params DiagnosticResult[] expected)
    {
        var cs = $$"""
            using Dapper;
            using System.Data.Common;

            [DapperAot(false)]
            class SomeCode
            {
                public void Foo(DbConnection conn) => conn.Execute(@"{{sql.Replace("\"", "\"\"")}}");
            }
            """;
        return CSVerifyAsync(cs, DefaultConfig, expected, SqlSyntax.SqlServer, debugModeFlags);
    }

    internal Task CSVerifyAsync(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer, ModeFlags debugModeFlags = ModeFlags.None)
        => base.CSVerifyAsync<TAnalyzer>(source, transforms, expected, sqlSyntax, debugModeFlags);

    new internal Task CSVerifyAsync<TCodeFix>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer, ModeFlags debugModeFlags = ModeFlags.None)
        where TCodeFix : CodeFixProvider, new()
        => CSVerifyAsync<TAnalyzer, TCodeFix>(source, transforms, expected, sqlSyntax, debugModeFlags);

    internal Task VBVerifyAsync(string source,
    Func<Solution, ProjectId, Solution>[] transforms,
    DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer)
    => base.VBVerifyAsync<TAnalyzer>(source, transforms, expected, sqlSyntax);

    new internal Task VBVerifyAsync<TCodeFix>(string source,
        Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer)
        where TCodeFix : CodeFixProvider, new()
        => VBVerifyAsync<TAnalyzer, TCodeFix>(source, transforms, expected, sqlSyntax);

}
public class Verifier<TAnalyzer, TCodeFix> : Verifier
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    protected Task CSVerifyAsync(string source, Func<Solution, ProjectId, Solution>[] transforms, 
        DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer)
        => CSVerifyAsync<TAnalyzer, TCodeFix>(source, transforms, expected, sqlSyntax);

    protected Task VBVerifyAsync(string source, Func<Solution, ProjectId, Solution>[] transforms,
        DiagnosticResult[] expected, SqlSyntax sqlSyntax = SqlSyntax.SqlServer)
        => VBVerifyAsync<TAnalyzer, TCodeFix>(source, transforms, expected, sqlSyntax);
}

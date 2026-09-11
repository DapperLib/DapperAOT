using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Dapper.AOT.Test;

/// <summary>
/// DAP057 reports the same fact at two different severities: leaving a call-site on vanilla
/// Dapper is a missed optimization under JIT and a latent publish-time crash under native AOT.
/// The interceptor goldens only cover the JIT case (no MSBuild properties in that harness), so
/// the promotion is pinned here.
/// </summary>
public class CommandDefinitionDiagnosticTests
{
    private const string Source = """
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]

        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                conn.Execute(new CommandDefinition("somesql"));
            }
        }
        """;

    [Theory]
    [InlineData(null, DiagnosticSeverity.Info)]          // no PublishAot: a missed optimization
    [InlineData("false", DiagnosticSeverity.Info)]
    [InlineData("true", DiagnosticSeverity.Warning)]     // PublishAot: this one will crash
    public void SeverityFollowsPublishAot(string? enableAotAnalyzer, DiagnosticSeverity expected)
    {
        var diagnostic = Assert.Single(Run(enableAotAnalyzer).Where(static d => d.Id == "DAP057"));
        Assert.Equal(expected, diagnostic.Severity);
        Assert.Contains("CommandDefinition", diagnostic.GetMessage());
    }

    private static ImmutableArray<Diagnostic> Run(string? enableAotAnalyzer)
    {
        var compilation = RoslynTestHelpers.CreateCompilation(Source, "assembly", "input.cs");
        var driver = CSharpGeneratorDriver.Create(
            [new DapperInterceptorGenerator().AsSourceGenerator()],
            parseOptions: RoslynTestHelpers.ParseOptionsLatestLangVer,
            optionsProvider: new OptionsProvider(enableAotAnalyzer));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        return diagnostics;
    }

    // the SDK sets EnableAotAnalyzer when PublishAot is on, and it is compiler-visible by
    // default; that is what the generator reads, so that is what the test supplies
    private sealed class OptionsProvider(string? enableAotAnalyzer) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(enableAotAnalyzer);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Options.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Options.Empty;

        private sealed class Options(string? enableAotAnalyzer) : AnalyzerConfigOptions
        {
            public static readonly Options Empty = new(null);

            public override bool TryGetValue(string key, out string value)
            {
                if (enableAotAnalyzer is not null && key == Dapper.CodeAnalysis.GlobalOptions.Keys.ProjectProperties_EnableAotAnalyzer)
                {
                    value = enableAotAnalyzer;
                    return true;
                }
                value = null!;
                return false;
            }

            public override IEnumerable<string> Keys => enableAotAnalyzer is null
                ? [] : [Dapper.CodeAnalysis.GlobalOptions.Keys.ProjectProperties_EnableAotAnalyzer];
        }
    }
}

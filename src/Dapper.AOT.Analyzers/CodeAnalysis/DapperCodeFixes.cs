using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.CodeAnalysis;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DapperCodeFixes)), Shared]

public sealed class DapperCodeFixes : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.CreateRange(new[]
    {
        DapperAnalyzer.Diagnostics.DapperAotNotEnabled.Id,
    });

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return; // any you want me to do what, exactly?

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic is null) continue; // GIGO
            if (!DiagnosticsBase.TryGetFieldName(diagnostic.Id, out var fieldName)) continue; // unknown

            bool firstThisDiagnostic = true;
            SyntaxToken token = default;

            switch (fieldName)
            {
                case nameof(DapperAnalyzer.Diagnostics.DapperAotNotEnabled):
                    Register("Enable AOT", GlobalEnableAOT, nameof(GlobalEnableAOT));
                    break;
            }

            void Register(string label, Func<CodeFixContext, SyntaxToken, CancellationToken, Task<Document>> fix, string key)
            {
                if (firstThisDiagnostic)
                {
                    token = root!.FindToken(diagnostic!.Location.SourceSpan.Start);
                    firstThisDiagnostic = false;
                }
                context.RegisterCodeFix(CodeAction.Create(label, c => fix(context, token, c), key), diagnostic);
            }
        }
    }

    private static Task<Document> GlobalEnableAOT(CodeFixContext context, SyntaxToken syntax, CancellationToken cancellation)
    {
        throw new NotImplementedException();
    }
}

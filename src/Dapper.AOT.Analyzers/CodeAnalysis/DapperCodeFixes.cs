using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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

    private static async Task<Document> GlobalEnableAOT(CodeFixContext context, SyntaxToken syntax, CancellationToken cancellation)
    {
        if (await context.Document.GetSyntaxRootAsync(cancellation).ConfigureAwait(false) is not CompilationUnitSyntax root)
        {
            return context.Document;
        }

        // already enabled? (also keeps fix-all idempotent within a document)
        foreach (var list in root.AttributeLists)
        {
            if (list.Target is null || !list.Target.Identifier.IsKind(SyntaxKind.ModuleKeyword)) continue;
            foreach (var attribute in list.Attributes)
            {
                switch (attribute.Name.ToString())
                {
                    case "DapperAot":
                    case "DapperAotAttribute":
                    case "Dapper.DapperAot":
                    case "Dapper.DapperAotAttribute":
                    case "global::Dapper.DapperAot":
                    case "global::Dapper.DapperAotAttribute":
                        return context.Document;
                }
            }
        }

        // fully qualified, so no using directive is needed
        var attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.ModuleKeyword)),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.ParseName("global::Dapper.DapperAot"))))
            .WithAdditionalAnnotations(Formatter.Annotation);

        // attribute lists print after extern aliases and usings, before everything else
        CompilationUnitSyntax newRoot;
        if (root.AttributeLists.Count != 0)
        {
            newRoot = root.InsertNodesBefore(root.AttributeLists[0], new[] { attributeList });
        }
        else if (root.Usings.Count != 0 || root.Externs.Count != 0)
        {
            // keep a blank line above the attribute, matching file layout conventions
            var newLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString();
            newRoot = root.AddAttributeLists(attributeList.WithLeadingTrivia(
                SyntaxFactory.EndOfLine(newLine.Length == 0 ? "\n" : newLine)));
        }
        else
        {
            // keep file-header trivia (license comments etc) above the attribute
            newRoot = root.WithLeadingTrivia()
                .AddAttributeLists(attributeList.WithLeadingTrivia(root.GetLeadingTrivia()));
        }

        return await Formatter.FormatAsync(context.Document.WithSyntaxRoot(newRoot),
            Formatter.Annotation, cancellationToken: cancellation).ConfigureAwait(false);
    }
}

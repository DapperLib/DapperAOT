using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DapperAOT.CodeAnalysis
{
    /// <summary>
    /// Parses the source for methods annotated with <c>Dapper.CommandAttribute</c>, generating an appropriate implementation.
    /// </summary>
    [Generator]
#pragma warning disable CS0618 // Type or member is obsolete
    public sealed class CommandGenerator : ISourceGenerator, ILoggingAnalyzer
#pragma warning restore CS0618 // Type or member is obsolete
    {
        sealed class CommandSyntaxReceiver : ISyntaxReceiver
        {
            public bool IsEmpty => _methods.Count == 0;
            private readonly List<MethodDeclarationSyntax> _methods = new();
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // when scanning the syntax tree, we'll keep things simple and gather
                // everything that looks likely, i.e. all partial methods with modifiers
                // and no body; we'll worry about what to include/exclude in the actual
                // generator, where we can get the full semantic model
                if (syntaxNode is MethodDeclarationSyntax method // is a method
                    && IsPartialWithAccessibility(method.Modifiers) // that is expecting an implementation
                    && method.Body is null && method.ExpressionBody is null // and isn't the half *with* an implementation
                    )
                {
                    _methods.Add(method);
                }
            }

            static bool IsPartialWithAccessibility(SyntaxTokenList modifiers)
                => modifiers.Any(SyntaxKind.PartialKeyword) &&
                (
                    modifiers.Any(SyntaxKind.PublicKeyword)
                || modifiers.Any(SyntaxKind.PrivateKeyword)
                || modifiers.Any(SyntaxKind.InternalKeyword)
                || modifiers.Any(SyntaxKind.ProtectedKeyword)
                );
            public List<MethodDeclarationSyntax>.Enumerator GetEnumerator()
                => _methods.GetEnumerator();
        }

        private event Action<string>? Log;
        event Action<string> ILoggingAnalyzer.Log
        {
            add => Log += value;
            remove => Log -= value;
        }

        void ISourceGenerator.Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not CommandSyntaxReceiver rec || rec.IsEmpty) return;
            var sb = new StringBuilder();
            foreach (var syntaxNode in rec)
            {
                try
                {
                    if (context.Compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetDeclaredSymbol(syntaxNode) is not IMethodSymbol method)
                        continue; // couldn't find it, or wasn't a method

                    if (method.PartialImplementationPart is not null) continue; // already has an implementation

                    var ca = method.GetAttributes().SingleOrDefault(x => IsCommandAttribute(x.AttributeClass));
                    if (ca is null) continue; // lacking [Command]

                    Log?.Invoke($"Detected candidate: '{method.Name}'");
                    sb.Append("// ").Append(method.Name).AppendLine();
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Error processing '{syntaxNode.Identifier}': '{ex.Message}'");
                    // TODO: declare a formal diagnostic for this
                    var err = Diagnostic.Create("DAP001", "Dapper", ex.Message, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 4, location: syntaxNode.GetLocation());
                    context.ReportDiagnostic(err);
                }
            }
            context.AddSource("DapperAOT.generated.cs", sb.ToString());

            static bool IsCommandAttribute(INamedTypeSymbol type)
                => type?.Name == "CommandAttribute" && type.ContainingNamespace?.Name == "Dapper";
        }

        void ISourceGenerator.Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(static () => new CommandSyntaxReceiver());
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
            List<(string Namespace, string TypeName, IMethodSymbol Method)>? candidates = null;
            foreach (var syntaxNode in rec)
            {
                try
                {
                    if (context.Compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetDeclaredSymbol(syntaxNode) is not IMethodSymbol method)
                        continue; // couldn't find it, or wasn't a method

                    if (method.PartialImplementationPart is not null) continue; // already has an implementation

                    if (method.IsGenericMethod) continue; // generics not implemented yet

                    var ca = method.GetAttributes().SingleOrDefault(x => IsCommandAttribute(x.AttributeClass));
                    if (ca is null) continue; // lacking [Command]

                    Log?.Invoke($"Detected candidate: '{method.Name}'");
                    (candidates ??= new()).Add((GetNamespace(method.ContainingType), GetTypeName(method.ContainingType), method));
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Error processing '{syntaxNode.Identifier}': '{ex.Message}'");
                    // TODO: declare a formal diagnostic for this
                    var err = Diagnostic.Create("DAP001", "Dapper", ex.Message, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 4, location: syntaxNode.GetLocation());
                    context.ReportDiagnostic(err);
                }
            }
            if (candidates is not null)
            {
                candidates.Sort((x, y) =>
                {
                    int delta = string.Compare(x.Namespace, y.Namespace, StringComparison.Ordinal);
                    if (delta == 0) delta = string.Compare(x.TypeName, y.TypeName, StringComparison.Ordinal);
                    return delta;
                });
                var generated = Generate(candidates);
                context.AddSource("DapperAOT.generated.cs", generated);
            }   

            static bool IsCommandAttribute(INamedTypeSymbol type)
                => type?.Name == "CommandAttribute" && type.ContainingNamespace?.Name == "Dapper";

            static string GetNamespace(INamedTypeSymbol? type)
            {
                static bool IsRoot([NotNullWhen(false)] INamespaceSymbol? ns)
                    => ns is null || ns.IsGlobalNamespace;

                var ns = type?.ContainingNamespace;
                if (IsRoot(ns)) return "";
                if (IsRoot(ns.ContainingNamespace)) return ns.Name;
                var sb = new StringBuilder();
                WriteAncestry(ns.ContainingNamespace, sb);
                sb.Append(ns.Name);
                return sb.ToString();

                static void WriteAncestry(INamespaceSymbol? ns, StringBuilder sb)
                {
                    if (!IsRoot(ns))
                    {
                        WriteAncestry(ns.ContainingNamespace, sb);
                        sb.Append(ns.Name).Append('.');
                    }
                }
            }
            static string GetTypeName(INamedTypeSymbol? type)
            {
                var parent = type?.ContainingType;
                if (parent is null) return type?.Name ?? "";
                var sb = new StringBuilder();
                WriteAncestry(parent, sb);
                sb.Append(type?.Name);
                return sb.ToString();

                static void WriteAncestry(INamedTypeSymbol? parent, StringBuilder sb)
                {
                    if (parent is not null)
                    {
                        WriteAncestry(parent.ContainingType, sb);
                        sb.Append(parent.Name).Append('.');
                    }
                }
            }
        }

        [SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "Clarity")]
        [SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Clarity")]
        private string Generate(List<(string Namespace, string TypeName, IMethodSymbol Method)> candidates)
        {

            var sb = new StringBuilder();
            int indent = 0;
            StringBuilder NewLine()
                => sb.AppendLine().Append('\t', indent);

            foreach (var nsGrp in candidates.GroupBy(x => x.Namespace))
            {
                Log?.Invoke($"Namespace '{nsGrp.Key}' has {nsGrp.Count()} candidate(s) in {nsGrp.Select(x => x.TypeName).Distinct().Count()} type(s)");

                if (!string.IsNullOrWhiteSpace(nsGrp.Key))
                {
                    NewLine().Append("namespace ").Append(nsGrp.Key);
                    NewLine().Append('{');
                    indent++;
                }

                foreach (var typeGrp in nsGrp.GroupBy(x => x.TypeName))
                {
                    int typeCount = 0;
                    ReadOnlySpan<char> span = typeGrp.Key;
                    int ix;
                    while ((ix = span.IndexOf('.')) > 0)
                    {
                        NewLine().Append("partial class ").Append(span.Slice(0, ix));
                        NewLine().Append('{');
                        indent++;
                        typeCount++;
                        span = span.Slice(ix + 1);
                    }
                    NewLine().Append("partial class ").Append(span);
                    NewLine().Append('{');
                    indent++;
                    typeCount++;

                    bool firstMethod = true;
                    foreach (var candidate in typeGrp)
                    {
                        if (!firstMethod)
                        {
                            NewLine();
                        }
                        firstMethod = false;
                        WriteMethod(candidate.Method, sb, indent);
                    }

                    while (typeCount > 0)
                    {
                        indent--;
                        typeCount--;
                        NewLine().Append('}');
                    }
                }

                if (!string.IsNullOrWhiteSpace(nsGrp.Key))
                {
                    indent--;
                    NewLine().Append('}');
                }
            }
            return sb.ToString();
        }

        private void WriteMethod(IMethodSymbol method, StringBuilder sb, int indent)
        {
            StringBuilder NewLine()
                => sb.AppendLine().Append('\t', indent);

            NewLine().Append(method.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Protected => "public",
                Accessibility.ProtectedOrInternal => "protected internal",
                _ => method.DeclaredAccessibility.ToString(), // expect this to cause an error, that's OK
            }).Append(' ').Append(method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)).Append(' ').Append(method.Name).Append("(");
            bool first = true;
            foreach (var p in method.Parameters)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(p.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
            sb.Append(")");
            NewLine().Append('{');
            indent++;

            NewLine().Append("// TODO");

            indent--;
            NewLine().Append('}');
                
        }

        void ISourceGenerator.Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(static () => new CommandSyntaxReceiver());
    }
}

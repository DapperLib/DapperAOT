using DapperAOT.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

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
                if (type is null) return "";
                var parent = type.ContainingType;
                if (parent is null && !IsGeneric(type)) return type.Name ?? "";
                var sb = new StringBuilder();
                WriteAncestry(type, sb, false);
                return sb.ToString();

                static bool IsGeneric(INamedTypeSymbol type)
                    => type.IsGenericType && !type.TypeArguments.IsDefaultOrEmpty;

                static void WriteType(INamedTypeSymbol type, StringBuilder sb)
                {
                    sb.Append(type.Name);
                    if (IsGeneric(type))
                    {
                        sb.Append('<');
                        bool first = true;
                        foreach (var t in type.TypeArguments)
                        {
                            if (!first) sb.Append(", ");
                            first = false;
                            sb.Append(t.Name);
                        }
                        sb.Append('>');
                    }
                }
                static void WriteAncestry(INamedTypeSymbol? type, StringBuilder sb, bool withSuffix)
                {
                    if (type is not null)
                    {
                        WriteAncestry(type.ContainingType, sb, true);
                        WriteType(type, sb);
                        if (withSuffix) sb.Append('.');
                    }
                }
            }
        }

        [SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "Clarity")]
        [SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Clarity")]
        private string Generate(List<(string Namespace, string TypeName, IMethodSymbol Method)> candidates)
        {

            var sb = CodeWriter.Create().Append("#nullable enable").NewLine();
            
            foreach (var nsGrp in candidates.GroupBy(x => x.Namespace))
            {
                var materializers = new Dictionary<ITypeSymbol, string>();
                Log?.Invoke($"Namespace '{nsGrp.Key}' has {nsGrp.Count()} candidate(s) in {nsGrp.Select(x => x.TypeName).Distinct().Count()} type(s)");

                if (!string.IsNullOrWhiteSpace(nsGrp.Key))
                {
                    sb.NewLine().Append("namespace ").Append(nsGrp.Key).Indent();
                }

                foreach (var typeGrp in nsGrp.GroupBy(x => x.TypeName))
                {
                    int typeCount = 0;
                    ReadOnlySpan<char> span = typeGrp.Key;
                    int ix;
                    while ((ix = span.IndexOf('.')) > 0)
                    {
                        sb.NewLine().Append("partial class ").Append(span.Slice(0, ix)).Indent();
                        typeCount++;
                        span = span.Slice(ix + 1);
                    }
                    sb.NewLine().Append("partial class ").Append(span).Indent();
                    typeCount++;

                    bool firstMethod = true;
                    foreach (var candidate in typeGrp)
                    {
                        if (!firstMethod) sb.NewLine();
                        firstMethod = false;
                        WriteMethod(candidate.Method, sb, materializers);
                    }

                    while (typeCount > 0)
                    {
                        sb.Outdent();
                        typeCount--;
                    }
                }

                if (!string.IsNullOrWhiteSpace(nsGrp.Key))
                {
                    sb.Outdent();
                }
            }
            return sb.ToString();
        }

        private void WriteMethod(IMethodSymbol method, CodeWriter sb, Dictionary<ITypeSymbol, string> materializers)
        {
            var attribs = method.GetAttributes();
            var text = TryGetCommandText(attribs, out var commandType);
            if (text is null)
            {
                Log?.Invoke($"No command-text resolved for '{method.Name}'");
                return;
            }

            string? connection = null, transaction = null;
            ITypeSymbol? connectionType = null, transactionType = null;
            foreach (var p in method.Parameters)
            {
                if (p.Type.IsReferenceType)
                {
                    if (p.Type.IsExact("System", "Data", "IDbConnection") || p.Type.IsKindOf("System", "Data", "Common", "DbConnection"))
                    {
                        if (connection is not null)
                        {
                            Log?.Invoke($"Multiple connection accessors found for '{method.Name}'");
                            return;
                        }
                        connection = p.Name;
                        connectionType = p.Type;
                    }
                    if (p.Type.IsExact("System", "Data", "IDbTransaction") || p.Type.IsKindOf("System", "Data", "Common", "DbTransaction"))
                    {
                        if (transaction is not null)
                        {
                            Log?.Invoke($"Multiple transaction accessors found for '{method.Name}'");
                            return;
                        }
                        transaction = p.Name;
                        transactionType = p.Type;
                    }
                }
            }
            if (connection is null && transaction is not null)
            {
                connection = transaction + "." + nameof(IDbTransaction.Connection);
                connectionType = ((IPropertySymbol)transactionType!.GetMembers(nameof(IDbTransaction.Connection)).Single()).Type;
            }

            if (connection is not null)
            {
                WriteMethodViaAdoDotNet(method, sb, materializers, connection, connectionType!, transaction, transactionType!, text, commandType);
            }
            // TODO: other APIs here
            else
            {
                Log?.Invoke($"No connection accessors found for '{method.Name}'");
                return;
            }

        }
        void WriteMethodViaAdoDotNet(IMethodSymbol method, CodeWriter sb, Dictionary<ITypeSymbol, string> materializers, string connection, ITypeSymbol connectionType, string? transaction, ITypeSymbol transactionType, string text, CommandType commandType)
        {
            sb.NewLine().Append(method.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Protected => "public",
                Accessibility.ProtectedOrInternal => "protected internal",
                _ => method.DeclaredAccessibility.ToString(), // expect this to cause an error, that's OK
            }).Append(" partial ").Append(method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(
                method.ReturnType.IsReferenceType && method.ReturnNullableAnnotation == NullableAnnotation.Annotated ? "? " : " ").Append(method.Name).Append("(");
            bool first = true;
            Log?.Invoke(TypeMetadata.Get(method).ToString());
            foreach (var p in method.Parameters)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(p.NullableAnnotation == NullableAnnotation.Annotated ? "? " : " ").Append(p.Name);

                //Log?.Invoke($"{p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} => {TypeMetadata.Get(p)}");
            }
            sb.Append(")");
            sb.Indent();

            // variable declarations
            const string LocalPrefix = "__dap__";
            var cmdType = GetMethodReturnType(connectionType, nameof(IDbConnection.CreateCommand));
            var readerType = GetMethodReturnType(cmdType, nameof(IDbCommand.ExecuteReader));
            sb.NewLine().Append(cmdType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append("? ").Append(LocalPrefix).Append("command = null;");
            sb.NewLine().Append(readerType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append("? ").Append(LocalPrefix).Append("reader = null;");
            sb.NewLine().Append("bool ").Append(LocalPrefix).Append("close = false;");
            sb.NewLine().Append("int[]? ").Append(LocalPrefix).Append("fieldNumbers = null;");
            sb.NewLine().Append("try");
            sb.Indent();
            // functional code
            sb.NewLine().Append("throw new global::System.NotImplementedException();");
            sb.Outdent();
            sb.NewLine().Append("finally");
            // cleanup code
            sb.Indent();
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("fieldNumbers is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(").Append(LocalPrefix).Append("fieldNumbers);");
            sb.NewLine().Append(LocalPrefix).Append("reader?.Dispose();");
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("command is not null)");
            sb.Indent();
            sb.NewLine().Append(LocalPrefix).Append("command.Connection = default;");
            sb.NewLine().Append(LocalPrefix).Append("command = global::System.Threading.Interlocked.Exchange(ref __dapper__Command_14142, ").Append(LocalPrefix).Append("command);");
            sb.NewLine().Append(LocalPrefix).Append("command?.Dispose();");
            sb.Outdent();
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("close) ").Append(connection).Append("?.Close();");
            sb.Outdent();

            //sb.NewLine().Append($"/* TODO; SQL is '{text}' */"); // this is just temp, not worried about escaping text

            sb.Outdent();

            static ITypeSymbol? GetMethodReturnType(ITypeSymbol? type, string name)
            {
                if (type is null) return null;
                ITypeSymbol? known = null;
                // if there are multiple with this name, we're fine - as long as the return type agrees
                foreach (ISymbol member in type.GetMembers(name))
                {
                    if (member is IMethodSymbol method)
                    {
                        if (known is null)
                        {
                            known = method.ReturnType;
                        }
                        else if (!SymbolEqualityComparer.Default.Equals(known, method.ReturnType))
                        {
                            return null!;
                        }
                    }
                }
                return known;
            }
        }

        void ISourceGenerator.Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(static () => new CommandSyntaxReceiver());

        static string? TryGetCommandText(ImmutableArray<AttributeData> attributes, out CommandType commandType)
        {
            foreach (var attrib in attributes)
            {
                if (IsCommandAttribute(attrib.AttributeClass))
                {
                    if (TryGetAttributeValue<string>(attrib, "CommandText", out var commandText))
                    {
                        if (TryGetAttributeValue<int>(attrib, "CommandType", out var rawValue))
                        {
                            commandType = (CommandType)rawValue;
                        }
                        else
                        {
                            commandType = commandText.IndexOf(' ') >= 0 ? CommandType.Text : CommandType.StoredProcedure;
                        }
                        return commandText;
                    }
                    break; // only expect one of these, so: give up
                }
            }
            commandType = default;
            return default;
        }

        internal static bool TryGetAttributeValue<T>(AttributeData? attrib, string name, [NotNullWhen(true)] out T? value)
        {
            if (attrib is not null)
            {
                // check named values first, since they take precedence semantically
                foreach (var na in attrib.NamedArguments)
                {
                    if (string.Equals(na.Key, name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (na.Value.Value is T typedNamedArgValue)
                        {
                            value = typedNamedArgValue;
                            return true;
                        }
                        break;
                    }
                }


                // check parameter values second
                var ctor = attrib.AttributeConstructor;
                var index = FindParameterIndex(ctor, "CommandText");
                if (index >= 0 && index < attrib.ConstructorArguments.Length && attrib.ConstructorArguments[index].Value is T typedCtorValue)
                {
                    value = typedCtorValue;
                    return true;
                }

                static int FindParameterIndex(IMethodSymbol? method, string name)
                {
                    if (method is not null)
                    {
                        int index = 0;
                        foreach (var p in method.Parameters)
                        {
                            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                                return index;
                            index++;
                        }
                    }
                    return -1;
                }
            }
            value = default;
            return false;
        }

        internal static bool IsCommandAttribute(ITypeSymbol? type)
            => type.IsExact("Dapper", "CommandAttribute");
    }
}

using Dapper.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using static Dapper.Internal.Utilities;

namespace Dapper.CodeAnalysis;

/// <summary>
/// Parses the source for methods annotated with <c>Dapper.CommandAttribute</c>, generating an appropriate implementation.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class CommandGenerator : ISourceGenerator
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

    /// <summary>
    /// Provide log feedback.
    /// </summary>
    public event Action<DiagnosticSeverity, string>? Log;

    /// <summary>
    /// Indicate version in generated code.
    /// </summary>
    public bool ReportVersion { get; set; } = true;

    /// <summary>
    /// The name of the file to generate.
    /// </summary>
    public string DefaultOutputFileName { get; set; } = "Dapper.generated.cs";

    void ISourceGenerator.Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not CommandSyntaxReceiver rec || rec.IsEmpty) return;
        List<(string Namespace, string TypeName, IMethodSymbol Method, MethodDeclarationSyntax Syntax)>? candidates = null;
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

                Log?.Invoke(DiagnosticSeverity.Info, $"Detected candidate: '{method.Name}'");
                (candidates ??= new()).Add((GetNamespace(method.ContainingType), GetTypeName(method.ContainingType), method, syntaxNode));
            }
            catch (Exception ex)
            {
                Log?.Invoke(DiagnosticSeverity.Error, $"Error processing '{syntaxNode.Identifier}': '{ex.Message}'");
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
            var generated = Generate(candidates, context);
            if (!string.IsNullOrWhiteSpace(generated))
            {
                context.AddSource(DefaultOutputFileName, generated);
            }
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

    static string GetVersion(Assembly assembly)
    {
        try
        {
            if (assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)) is AssemblyFileVersionAttribute afva
                && !string.IsNullOrWhiteSpace(afva.Version))
            {
                return afva.Version.Trim();
            }
        }
        catch { }
        return assembly.GetName().Version.ToString();
    }
#pragma warning disable IDE0079 // unnecessary suppression - is framework dependent
    [SuppressMessage("Style", "IDE0042:Deconstruct variable declaration", Justification = "Clarity")]
    [SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Clarity")]
#pragma warning restore IDE0079
    private string Generate(List<(string Namespace, string TypeName, IMethodSymbol Method, MethodDeclarationSyntax Syntax)> candidates, in GeneratorExecutionContext context)
    {
        var generatorType = GetType();
        var sb = CodeWriter.Create().Append("#nullable enable")
            .NewLine().Append("//------------------------------------------------------------------------------")
            .NewLine().Append("// <auto-generated>")
            .NewLine().Append("// This code was generated by:")
            .NewLine().Append("//     ").Append(generatorType.FullName).Append(" v").Append(ReportVersion ? GetVersion(generatorType.Assembly) : "N/A")
            .NewLine().Append("// Changes to this file may cause incorrect behavior and")
            .NewLine().Append("// will be lost if the code is regenerated.")
            .NewLine().Append("// </auto-generated>")
            .NewLine().Append("//------------------------------------------------------------------------------")
            .NewLine().Append("#region Designer generated code");

        int totalWritten = 0;
        MaterializerTracker materializers = new("Dapper.Internal." + LocalPrefix + context.Compilation.AssemblyName + "_TypeReaders");

        foreach (var nsGrp in candidates.GroupBy(x => x.Namespace))
        {
            Log?.Invoke(DiagnosticSeverity.Info, $"Namespace '{nsGrp.Key}' has {nsGrp.Count()} candidate(s) in {nsGrp.Select(x => x.TypeName).Distinct().Count()} type(s)");

            if (!string.IsNullOrWhiteSpace(nsGrp.Key))
            {
                sb.NewLine().Append("namespace ").Append(nsGrp.Key).Indent();
            }

            foreach (var typeGrp in nsGrp.GroupBy(x => x.TypeName))
            {
                int typeCount = 0;
                ReadOnlySpan<char> span = typeGrp.Key.AsSpan();
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
                    var rollback = sb.Length;
                    if (WriteMethod(candidate.Method, candidate.Syntax, sb, materializers, context))
                    {
                        totalWritten++;
                    }
                    else
                    {   // revert in case of any partial writes
                        sb.Length = rollback;
                    }
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

        if (materializers.Count != 0)
        {
            sb.NewLine().NewLine().Append("namespace ").Append(materializers.Namespace).Indent();
            string helperAccessibility = "internal";
            if (context.ParseOptions is CSharpParseOptions csOpt && csOpt.LanguageVersion >= (LanguageVersion)1100)
            {
                helperAccessibility = "file";
            }

            foreach (var genType in materializers.Symbols.OrderBy(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
            {
                var members = BuildMembers(genType, context);
                // if we have multiple types with the same name, we'll need to disambiguate them in our code
                var readerName = materializers.GetMaterializerName(genType);
                sb.NewLine().Append(helperAccessibility).Append(" sealed class ").Append(readerName).Append(" : global::Dapper.TypeReader<").Append(genType).Append(">").Indent();
                sb.NewLine().Append("private ").Append(readerName).Append("() { }");
                sb.NewLine().Append("public static readonly ").Append(readerName).Append(" Instance = new();");

                if (members.Length > 0)
                {
                    sb.NewLine()
                        .NewLine().Append("/// <inheritdoc/>")
                        .NewLine().Append("public override int GetToken(string columnName)").Indent();
                    sb.DisableObsolete().NewLine().Append("switch (global::Dapper.Internal.InternalUtilities.NormalizedHash(columnName))").Indent();
                    foreach (var grp in members.GroupBy(x => x.Hash).OrderBy(x => x.Key))
                    {
                        sb.NewLine().Append("case ").Append((int)grp.Key).Append("U:").Indent(false);
                        foreach (var el in grp)
                        {
                            sb.NewLine().Append("if (global::Dapper.Internal.InternalUtilities.NormalizedEquals(columnName, ").AppendVerbatimLiteral(InternalUtilities.Normalize(el.ColumnName)).Append(")) return ").Append(el.Token).Append(';');
                        }
                        sb.NewLine().Append("break;").Outdent(false);
                    }
                    sb.Outdent().RestoreObsolete();
                    sb.NewLine().Append("return -1;").Outdent(); // GetToken
                }
                

                sb.NewLine()
                    .NewLine().Append("/// <inheritdoc/>")
                    .NewLine().Append("public override int GetToken(int token, global::System.Type type, bool isNullable) => token;");

                sb.NewLine()
                    .NewLine().Append("/// <inheritdoc/>")
                    .NewLine().Append("public override ").Append(genType).Append(" Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)").Indent();
                sb.NewLine().Append(genType).Append(" obj = new();");
                if (members.Length > 0)
                {
                    sb.NewLine().Append("for (int i = 0; i < tokens.Length; i++)").Indent().NewLine().Append("switch (tokens[i])").Indent();
                    foreach (var member in members)
                    {
                        sb.NewLine().Append("case ").Append(member.Token);
                        if (DoNullCheck(member.Type))
                        {
                            sb.Append(" when reader.IsDBNull(columnOffset + i)");
                        }
                        sb.Append(':').Indent(false);
                        sb.NewLine().Append("obj.").Append(member.Member.Name).Append(" = ");
                        var method = GetReadMethod(member.Type);
                        if (method is null)
                        {
                            sb.Append("reader.GetFieldValue<").Append(member.Type).Append(">(columnOffset + i);");
                        }
                        else
                        {
                            sb.Append("reader.").Append(method).Append("(columnOffset + i);");
                        }
                        sb.NewLine().Append("break;").Outdent(false);
                    }
                    sb.Outdent().Outdent();
                }
                sb.NewLine().Append("return obj;").Outdent() // Read
                    .Outdent(); // serializer type
            }

            sb.Outdent();
        }
        if (totalWritten == 0) sb.Length = 0;
        else sb.NewLine().Append("#endregion");
        return sb.ToStringRecycle();

        static bool DoNullCheck(ITypeSymbol type) => type.NullableAnnotation switch
        {
            NullableAnnotation.Annotated => true,
            NullableAnnotation.NotAnnotated => false,
            NullableAnnotation.None => !type.IsValueType,
            _ => true,
        };
        static string? GetReadMethod(ITypeSymbol type)
        {
            switch(type.SpecialType)
            {
                case SpecialType.System_String: return "GetString";
                case SpecialType.System_Int32: return "GetInt32";
                case SpecialType.System_Int64: return "GetInt64";
                case SpecialType.System_Int16: return "GetInt16";
                case SpecialType.System_Byte: return "GetByte";
                case SpecialType.System_Char: return "GetChar";
                case SpecialType.System_Single: return "GetFloat";
                case SpecialType.System_Double: return "GetDouble";
                case SpecialType.System_Decimal: return "GetDecimal";
                case SpecialType.System_DateTime: return "GetDateTime";
                case SpecialType.System_Boolean: return "GetBoolean";
            };
            if (type.IsExact("System", "Guid")) return "GetGuid";
            return null;
        }
    }

    private (string ColumnName, uint Hash, int Token, ISymbol Member, ITypeSymbol Type)[] BuildMembers(ITypeSymbol genType, GeneratorExecutionContext context)
    {
        var genMembers = genType.GetMembers();
        var members = new List<(string ColumnName, uint Hash, int Token, ISymbol Member, ITypeSymbol Type)>(genMembers.Length);
        foreach (var member in genMembers)
        {
            if (member is null || member.IsStatic) continue;
            ITypeSymbol valueType;
            switch (member.Kind)
            {
                case SymbolKind.Field when member is IFieldSymbol field && field.AssociatedSymbol is null // not a prop-backer
                    && (valueType = field.Type) is not null:
                case SymbolKind.Property when member is IPropertySymbol prop
                    && (valueType = prop.Type) is not null:
                    var attribs = member.GetAttributes();
                    if (NotMapped(attribs)) continue;
                    bool isExplicit = IsExplicitColumn(attribs, out var name);
                    if (!(isExplicit || member.DeclaredAccessibility == Accessibility.Public)) continue;
                    if (string.IsNullOrWhiteSpace(name)) name = member.Name;
                    members.Add((name!, InternalUtilities.NormalizedHash(name),
                        members.Count, member, valueType));
                    break;
            }
        }
        return members.ToArray();

        static bool NotMapped(ImmutableArray<AttributeData> attribs)
        {
            foreach (var attrib in attribs)
            {
                if (attrib.AttributeClass.IsExact("System", "ComponentModel", "DataAnnotations", "Schema", "NotMappedAttribute")) return true;
            }
            return false;
        }
        static bool IsExplicitColumn(ImmutableArray<AttributeData> attribs, out string? name)
        {
            foreach (var attrib in attribs)
            {
                if (attrib.AttributeClass.IsExact("System", "ComponentModel", "DataAnnotations", "Schema", "ColumnAttribute"))
                {
                    attrib.TryGetAttributeValue<string>("name", out name);
                    return true;
                }
            }
            name = null;
            return false;
        }
    }

    private bool WriteMethod(IMethodSymbol method, MethodDeclarationSyntax syntax, CodeWriter sb, MaterializerTracker materializers, in GeneratorExecutionContext context)
    {
        var attribs = method.GetAttributes();
        var text = TryGetCommandText(attribs, out var commandType);
        if (text is null) return false;
        bool textDirect = true;

        string? connection = null, transaction = null, cancellationToken = null;
        ITypeSymbol? connectionType = null, transactionType = null;
        foreach (var p in method.Parameters)
        {
            var type = GetHandledType(p.Type);
            switch (type)
            {
                case HandledType.Connection:

                    if (connection is not null)
                    {
                        Log?.Invoke(DiagnosticSeverity.Error, $"Multiple connection accessors found for '{method.Name}'");
                        return false;
                    }
                    connection = p.Name;
                    connectionType = p.Type;
                    break;
                case HandledType.Transaction:
                    if (transaction is not null)
                    {
                        Log?.Invoke(DiagnosticSeverity.Error, $"Multiple transaction accessors found for '{method.Name}'");
                        return false;
                    }
                    transaction = p.Name;
                    transactionType = p.Type;
                    break;
                case HandledType.CancellationToken:
                    if (cancellationToken is not null)
                    {
                        Log?.Invoke(DiagnosticSeverity.Error, $"Multiple cancellation tokens found for '{method.Name}'");
                        return false;
                    }
                    cancellationToken = p.Name;
                    break;
                case HandledType.None when TryGetCommandText(p.GetAttributes(), out _) is string ignoreCmdText:
                    if (!string.IsNullOrWhiteSpace(ignoreCmdText))
                    {
                        Log?.Invoke(DiagnosticSeverity.Error, $"Fixed command-text should not be specified against parameters for '{method.Name}'");
                    }
                    text = p.Name;
                    textDirect = false;
                    break;
            }
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            Log?.Invoke(DiagnosticSeverity.Error, $"No command-text resolved for '{method.Name}'");
            return false;
        }
        if (transaction is not null)
        {
            if (connection is null)
            {
                connection = transaction + "?." + nameof(IDbTransaction.Connection);
                connectionType = ((IPropertySymbol)transactionType!.GetMembers(nameof(IDbTransaction.Connection)).Single()).Type;
            }
            else
            {
                connection = connection + " ?? " + transaction + "?." + nameof(IDbTransaction.Connection);
            }
        }
        if (cancellationToken is null)
        {
            cancellationToken = "global::System.Threading.CancellationToken.None";
        }


        if (connection is not null)
        {
            return WriteMethodViaAdoDotNet(method, syntax, sb, materializers, connection, connectionType!, transaction, transactionType!, text, textDirect,
                commandType, context, cancellationToken);
        }
        // TODO: other APIs here
        else
        {
            Log?.Invoke(DiagnosticSeverity.Error, $"No connection accessors found for '{method.Name}'");
            return false;
        }
    }

    private enum HandledType
    {
        None,
        Connection,
        Transaction,
        CancellationToken,
    }
    static HandledType GetHandledType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.Arity == 0)
        {
            if (type.IsReferenceType)
            {
                if (type.IsExact("System", "Data", "IDbConnection", 0) || type.IsKindOf("System", "Data", "Common", "DbConnection", 0))
                    return HandledType.Connection;
                if (type.IsExact("System", "Data", "IDbTransaction", 0) || type.IsKindOf("System", "Data", "Common", "DbTransaction", 0))
                    return HandledType.Transaction;
            }
            else
            {
                if (type.IsExact("System", "Threading", "CancellationToken", 0))
                    return HandledType.CancellationToken;
            }
        }
        return HandledType.None;
    }

    static string? GetLocation(IMethodSymbol method, out string? identifier)
    {
        var locations = method.Locations;
        if (!locations.IsDefaultOrEmpty)
        {
            foreach (var loc in locations)
            {
                if (loc.IsInSource)
                {
                    var sp = loc.GetLineSpan();
                    var line = sp.StartLinePosition.Line.ToString(CultureInfo.InvariantCulture);
                    identifier = Regex.Replace(sp.Path + "_" + method.Name + "_" + line, @"[^a-zA-Z0-9_]", "_");
                    return method.ContainingType.Name + "." + method.Name + ", " + sp.Path.Replace('\\','/') + " #" + line;
                }
            }
        }
        identifier = null;
        return null;
    }

    static DbType? GetDbType(IParameterSymbol parameter, out int? size, ITypeSymbol? type = null)
    {
        size = default;
        type ??= parameter.Type;
        DbType? dbType = type.SpecialType switch
        {
            SpecialType.System_UInt16 => DbType.UInt16,
            SpecialType.System_Int16 => DbType.Int16,
            SpecialType.System_Int32 => DbType.Int32,
            SpecialType.System_UInt32 => DbType.UInt32,
            SpecialType.System_Int64 => DbType.Int64,
            SpecialType.System_UInt64 => DbType.UInt64,
            SpecialType.System_Single => DbType.Single,
            SpecialType.System_Double => DbType.Double,
            SpecialType.System_Boolean => DbType.Boolean,
            SpecialType.System_Byte => DbType.Byte,
            SpecialType.System_SByte => DbType.SByte,
            SpecialType.System_DateTime => DbType.DateTime,
            SpecialType.System_String => DbType.String,
            _ => default(DbType?),
        };
        if (dbType is null)
        {
            if (type.IsExact("System", "DateTimeOffset", 0))
                dbType = DbType.DateTimeOffset;
            else if (type.IsExact("System", "Guid", 0))
                dbType = DbType.Guid;
        }

        if (dbType == DbType.String)
        {
            size = -1;
        }
        return dbType;
    }

    static void AddIfMissing(CodeWriter sb, string attribute, in GeneratorExecutionContext context, IMethodSymbol? method)
    {
        var found = context.Compilation.GetTypeByMetadataName(attribute);
        if (found is not null && (method is null || !method.IsDefined(found)))
        {
            sb.NewLine().Append("[").Append(found).Append("]");
        }
    }

    static bool WriteMethodHeader(CodeWriter sb, IMethodSymbol method, MethodDeclarationSyntax syntax, QueryFlags flags, bool asInnerIteratorImpl, in GeneratorExecutionContext context)
    {
        bool needsCancellationToken = !asInnerIteratorImpl && flags.IsAsync() && flags.Has(QueryFlags.IsIterator);
        if (needsCancellationToken)
        {
            // for async iterators, we need to know whether there is a CT on the method
            // (if there is, we can ensure it is an enumerator cancellation, but if there
            // isn't, we'll need to add a wrapper method)

            foreach (var p in method.Parameters)
            {
                if (GetHandledType(p.Type) == HandledType.CancellationToken)
                {
                    needsCancellationToken = false;
                    break;
                }
            }
        }
        LanguageVersion langVer = context.ParseOptions is CSharpParseOptions csOpt ? csOpt.LanguageVersion : 0;

        AddIfMissing(sb, "System.Diagnostics.DebuggerNonUserCodeAttribute", context, method);
        if (context.AllowUnsafe()) AddIfMissing(sb, "System.Runtime.CompilerServices.SkipLocalsInitAttribute", context, method);
        if (langVer >= (LanguageVersion)1000 && flags.IsAsync() && !flags.Has(QueryFlags.IsIterator) && method.ReturnType.IsValueType)
        {
            if (method.ReturnType is INamedTypeSymbol ntret && (ntret.Arity is 0 or 1)
                 && method.ReturnType.IsExact("System", "Threading", "Tasks", "ValueTask", ntret.Arity)
                && context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder") is INamedTypeSymbol builder
                && context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute") is INamedTypeSymbol aba
                && !method.IsDefined(aba))
            {
                sb.NewLine().Append("[").Append(aba).Append("(typeof(").Append(builder);
                if (ntret.Arity == 1) sb.Append("<>");
                sb.Append("))]");
            }
        }

        if (asInnerIteratorImpl)
        {
            sb.NewLine().Append("static async ");
        }
        else
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
            });

            if (syntax.Modifiers.Any(SyntaxKind.NewKeyword)) sb.Append(" new"); // can't find this on method!
            if (method.IsStatic) sb.Append(" static");
            if (method.IsOverride) sb.Append(" override");
            if (method.IsVirtual) sb.Append(" virtual");
            if (method.IsSealed) sb.Append(" sealed");
            if (flags.IsAsync() && !needsCancellationToken) sb.Append(" async");

            sb.Append(" partial ");
        }

        sb.Append(method.ReturnType).Append(
            method.ReturnType.IsReferenceType && method.ReturnNullableAnnotation == NullableAnnotation.Annotated ? "? " : " ")
            .Append(asInnerIteratorImpl ? LocalPrefix : "").Append(method.Name).Append("(");

        bool first = true;
        foreach (var p in method.Parameters)
        {
            if (first)
            {
                if (method.IsExtensionMethod && !asInnerIteratorImpl) sb.Append("this ");
                first = false;
            }
            else
            {
                sb.Append(", ");
            }
            if (GetHandledType(p.Type) == HandledType.CancellationToken)
            {
                if (flags.IsAsync() && flags.Has(QueryFlags.IsIterator) && !p.IsEnumeratorCancellationToken())
                {
                    // we can add it
                    sb.Append("[global::System.Runtime.CompilerServices.EnumeratorCancellationAttribute] ");
                }
            }
            sb.Append(p.Type).Append(p.NullableAnnotation == NullableAnnotation.Annotated ? "? " : " ").Append(p.Name);

        }
        if (asInnerIteratorImpl)
        {
            if (!first) sb.Append(", ");
            sb.Append("[global::System.Runtime.CompilerServices.EnumeratorCancellationAttribute] global::System.Threading.CancellationToken ").Append(LocalPrefix).Append("cancellation");
        }
        sb.Append(")");
        return needsCancellationToken;
    }

    const string LocalPrefix = "__dapper__";

    static bool UseCommandField(string? identifier, bool commandTextAsLiteral)
    {
        if (!commandTextAsLiteral) return false; // only re-use fixed-text commands
        if (string.IsNullOrWhiteSpace(identifier)) return false; // needs an identifier

        // maps?

        return true;
    }
    bool WriteMethodViaAdoDotNet(IMethodSymbol method, MethodDeclarationSyntax syntax, CodeWriter sb, MaterializerTracker materializers, string connection, ITypeSymbol connectionType, string? transaction, ITypeSymbol transactionType, string commandText, bool commandTextAsLiteral, CommandType commandType, in GeneratorExecutionContext context, string cancellationToken)
    {
        var cmdType = GetMethodReturnType(connectionType, nameof(IDbConnection.CreateCommand));
        if (cmdType is null)
        {
            Log?.Invoke(DiagnosticSeverity.Error, $"Unable to resolve command-type for '{method.Name}'");
            return false;
        }
        var category = CategorizeQuery(method, context);
        ITypeSymbol? readerType = null;
        if (category.Flags.Has(QueryFlags.IsQuery))
        {
            readerType = GetMethodReturnType(cmdType, nameof(IDbCommand.ExecuteReader));
            if (readerType is null)
            {
                Log?.Invoke(DiagnosticSeverity.Error, $"Unable to resolve reader-type for '{method.Name}'");
                return false;
            }
        }

        category.ConsiderGeneratingTypeReader(materializers);

        var location = GetLocation(method, out string? identifier);
        var commandField = UseCommandField(identifier, commandTextAsLiteral) ? ("s_" + LocalPrefix + "command_" + identifier) : null;

        sb.NewLine();
        if (commandField is not null)
        {
            sb.NewLine().Append("// available inactive command for ").Append(method.Name).Append(" (interlocked)")
                .NewLine().Append("private static ").Append(cmdType).Append("? ").Append(commandField).Append(";").NewLine();
        }
        bool needsInnerImpl = WriteMethodHeader(sb, method, syntax, category.Flags, false, context);
        sb.Indent();

        if (needsInnerImpl)
        {
            sb.NewLine().Append("// use wrapper method to add support for enumerator cancellation");
            sb.NewLine().Append("return ").Append(LocalPrefix).Append(method.Name).Append("(");
            bool first = true;
            foreach (var p in method.Parameters)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(p.Name);
            }
            // and include a dummy CT
            if (!first) sb.Append(", global::System.Threading.CancellationToken.None);");
            sb.NewLine();
            WriteMethodHeader(sb, method, syntax, category.Flags, true, context);
            sb.Indent();
            cancellationToken = LocalPrefix + "cancellation";
        }

        // variable declarations
        sb.NewLine().Append("// locals");
        sb.NewLine().Append(cmdType).Append("? ").Append(LocalPrefix).Append("command = null;");
        if (readerType is not null) sb.NewLine().Append(readerType).Append("? ").Append(LocalPrefix).Append("reader = null;");
        sb.NewLine().Append("bool ").Append(LocalPrefix).Append("close = false;");
        bool useFieldTokens = category.Has(QueryFlags.IsQuery);
        if (useFieldTokens)
        {
            sb.NewLine().Append("int[]? ").Append(LocalPrefix).Append("tokenBuffer = null;");
        }

        if (category.NeedsListLocal())
        {
            if (category.UseCollector()) sb.DisableObsolete();
            sb.NewLine().Append(category.ListType).Append(" ").Append(LocalPrefix).Append("result").Append(
                category.UseCollector() ? " = default;" : ";");
            if (category.UseCollector()) sb.RestoreObsolete();
        }

        sb.NewLine().Append("try");
        sb.Indent();

        // prepare connection
        sb.NewLine().Append("// prepare connection");
        sb.NewLine().Append("if (").Append(connection).Append("!.State == global::System.Data.ConnectionState.Closed)").Indent();
        if (category.IsAsync())
        {
            sb.NewLine().Append("await ").Append(connection).Append("!.OpenAsync(").Append(cancellationToken).Append(").ConfigureAwait(false);");
        }
        else
        {
            sb.NewLine().Append(connection).Append("!.Open();");
        }
        sb.NewLine().Append(LocalPrefix).Append("close = true;").Outdent();

        // prepare command (excluding parameter values)
        sb.NewLine().NewLine().Append("// prepare command (excluding parameter values)");
        if (commandField is not null)
        {
            sb.NewLine().Append("if ((").Append(LocalPrefix).Append("command = global::System.Threading.Interlocked.Exchange(ref ").Append(commandField).Append(", null)) is null)");
            sb.Indent();
        }
        sb.NewLine().Append(LocalPrefix).Append("command = ").Append(LocalPrefix).Append("CreateCommand(").Append(connection).Append("!");
        if (!commandTextAsLiteral) sb.Append(", ").Append(commandText); // append the evaluated expression
        sb.Append(");");
        if (commandField is not null)
        {
            sb.Outdent().NewLine().Append("else").Indent().NewLine().Append(LocalPrefix).Append("command.Connection = ").Append(connection).Append(";").Outdent();
        }

        static bool IsDictionary(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? valueType)
        {
            if (type is INamedTypeSymbol named && (named.IsExact("System", "Collections", "Generic", "Dictionary", 2) || named.IsExact("System", "Collections", "Generic", "IDictionary", 2))
                && named.TypeArguments[0].SpecialType == SpecialType.System_String)
            {
                valueType = named.TypeArguments[1];
                return true;
            }
            valueType = null;
            return false;
        }

        // assign parameter values
        int index = 0;
        var allBasic = !method.Parameters.Any(p => IsDictionary(p.Type, out _));
        foreach (var p in method.Parameters)
        {
            if (GetHandledType(p.Type) != HandledType.None) continue;
            if (TryGetCommandText(p.GetAttributes(), out _) is not null) continue; // ignore commands
            if (index == 0)
            {
                sb.NewLine().NewLine().Append("// assign parameter values");
                sb.DisableObsolete();
            }
            if (IsDictionary(p.Type, out _)) continue;
            sb.NewLine().Append(LocalPrefix).Append("command.Parameters[").Append(index).Append("].Value = global::Dapper.Internal.InternalUtilities.AsValue(").Append(p.Name).Append(");");
            index++;
        }
        if (!allBasic)
        {
            foreach (var p in method.Parameters)
            {
                if (GetHandledType(p.Type) != HandledType.None) continue;
                if (TryGetCommandText(p.GetAttributes(), out _) is not null) continue; // ignore commands
                if (!IsDictionary(p.Type, out var valueType)) continue;
                {
                    var type = GetDbType(p, out var size, valueType);
                    sb.NewLine().Append("if (").Append(p.Name).Append(" is not null)").Indent();
                    sb.NewLine().Append("var ").Append(LocalPrefix).Append("args = command.Parameters;");
                    sb.NewLine().Append("foreach (var ").Append(LocalPrefix).Append("pair in ").Append(p.Name).Append(")").Indent();
                    sb.NewLine().Append("var ").Append(LocalPrefix).Append("p = ").Append(LocalPrefix).Append("command.CreateParameter();");
                    sb.NewLine().Append(LocalPrefix).Append("p.ParameterName = ").Append(LocalPrefix).Append("pair.Key;");
                    sb.NewLine().Append(LocalPrefix).Append("p.Direction = global::System.Data.ParameterDirection.Input;");
                    if (type is not null)
                    {
                        sb.NewLine().Append(LocalPrefix).Append("p.DbType = global::System.Data.DbType.").Append(type.ToString()).Append(";");
                    }
                    if (size is not null)
                    {
                        sb.NewLine().Append(LocalPrefix).Append("p.Size = ").Append(size.GetValueOrDefault()).Append(";");
                    }
                    sb.NewLine().Append(LocalPrefix).Append("p.Value = global::Dapper.Internal.InternalUtilities.AsValue(").Append(LocalPrefix).Append("pair.Value);");
                    sb.NewLine().Append(LocalPrefix).Append("args.Add(").Append(LocalPrefix).Append("p);");
                    sb.Outdent().Outdent();
                }
            }
        }

        if (index != 0) sb.RestoreObsolete();
        sb.NewLine();

        if (category.Has(QueryFlags.IsQuery) && category.ItemType is not null)
        {
            if (category.Has(QueryFlags.IsSingle))
            {
                ExecuteReaderSingle(sb, category.Flags, category.ItemType, cancellationToken, materializers);
            }
            else
            {
                ExecuteReaderMultiple(sb, category, cancellationToken);
            }
        }
        else if (category.Has(QueryFlags.IsScalar) && category.ItemType is not null)
        {
            // TODO: scalars
            sb.NewLine().Append("#error Scalar not implemented");
        }
        else
        {
            // non-query
            ExecuteNonQuery(sb, category.Flags, cancellationToken);
        }

        // TODO: post-process parameters
        sb.NewLine().NewLine().Append("// TODO: post-process parameters").NewLine();

        if (category.NeedsListLocal())
        {
            sb.NewLine().Append("// return rowset");
            switch (category.ListStrategy)
            {
                case ListStrategy.SimpleList:
                    sb.NewLine().Append("return ").Append(LocalPrefix).Append("result;");
                    break;
                case ListStrategy.Array:
                    if (category.HasListMethod("ToArray"))
                    {
                        sb.NewLine().Append("return ").Append(LocalPrefix).Append("result.ToArray();");
                    }
                    else
                    {
                        sb.NewLine().Append("return ").Append(LocalPrefix).Append("result.Span.ToArray();");
                    }
                    break;
                case ListStrategy.ImmutableArray:
                    if (category.HasListMethod("ToImmutableArray"))
                    {
                        sb.NewLine().Append("return ").Append(LocalPrefix).Append("result.ToImmutableArray();");
                    }
                    else
                    {
                        sb.NewLine().Append("return global::System.Collections.Immutable.ImmutableArray.Create<").Append(category.ItemType).Append(">(")
                        .Append(LocalPrefix).Append("result.GetBuffer(), 0, ").Append(LocalPrefix).Append("result.Count);");
                    }
                    break;
                default:
                    if (category.HasListMethod("ToImmutableList"))
                    {
                        sb.NewLine().Append("return ").Append(LocalPrefix).Append("result.ToImmutableList();");
                    }
                    else
                    {
                        sb.NewLine().Append("switch (").Append(LocalPrefix).Append("result.Count)").Indent();
                        sb.NewLine().Append("case 0:").Indent(false).Append("return global::System.Collections.Immutable.ImmutableList<")
                            .Append(category.ItemType).Append(">.Empty;").Outdent(false);
                        sb.NewLine().Append("case 1:").Indent(false).Append("return global::System.Collections.Immutable.ImmutableList.Create<")
                            .Append(category.ItemType).Append(">(").Append(LocalPrefix).Append("result[0]").Append(");").Outdent(false);
                        sb.NewLine().Append("default:").Indent(false).Append("return global::System.Collections.Immutable.ImmutableList.CreateRange<")
                            .Append(category.ItemType).Append(">(").Append(LocalPrefix).Append("result.Segment").Append(");").Outdent(false);
                        sb.Outdent();
                    }
                    break;
            }
        }

        sb.Outdent();
        sb.NewLine().Append("finally");
        // cleanup code
        sb.Indent();
        sb.NewLine().Append("// cleanup");
        if (useFieldTokens)
        {
            sb.NewLine().Append("global::Dapper.TypeReader.Return(ref ").Append(LocalPrefix).Append("tokenBuffer);");
        }
        if (category.UseCollector())
        {
            sb.NewLine().Append(LocalPrefix).Append("result.Dispose();");
        }
        if (readerType is not null)
        {
            if (category.IsAsync() && readerType.HasDisposeAsync())
            {
                sb.NewLine().Append("if (").Append(LocalPrefix).Append("reader is not null) ").Append("await ").Append(LocalPrefix).Append("reader.DisposeAsync().ConfigureAwait(false);");
            }
            else
            {
                sb.NewLine().Append(LocalPrefix).Append("reader?.Dispose();");
            }
        }
        sb.NewLine().Append("if (").Append(LocalPrefix).Append("command is not null)");
        sb.Indent();
        if (commandField is not null)
        {
            sb.NewLine().Append(LocalPrefix).Append("command.Connection = default;");
            sb.NewLine().Append(LocalPrefix).Append("command = global::System.Threading.Interlocked.Exchange(ref ").Append(commandField).Append(", ").Append(LocalPrefix).Append("command);");
        }
        if (category.IsAsync() && cmdType.HasDisposeAsync())
        {
            sb.NewLine();
            if (commandField is not null)
            {   // it could have become null via the interlock, so guard against that
                sb.Append("if (").Append(LocalPrefix).Append("command is not null) ");
            }
            sb.Append("await ").Append(LocalPrefix).Append("command.DisposeAsync().ConfigureAwait(false);");
        }
        else
        {
            sb.NewLine().Append(LocalPrefix).Append(commandField is null ? "command.Dispose();" : "command?.Dispose();");
        }
        sb.Outdent();

        if (category.IsAsync() && connectionType.HasBasicAsyncMethod("CloseAsync", out var awaitableType, out bool cancellable) && !cancellable)
        {
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("close) await (").Append(connection).Append("?.CloseAsync() ?? ")
                .Append(awaitableType.IsValueType ? "default" : "global::System.Threading.Tasks.Task.CompletedTask")
                .Append(").ConfigureAwait(false);");
        }
        else
        {
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("close) ").Append(connection).Append("?.Close();");
        }
        sb.Outdent();

        if (needsInnerImpl)
        {
            sb.Outdent();
        }

        // command initialization
        sb.NewLine().NewLine().Append("// command factory for ").Append(method.Name);
        AddIfMissing(sb, "System.Diagnostics.DebuggerNonUserCodeAttribute", context, method);
        if (context.AllowUnsafe()) AddIfMissing(sb, "System.Runtime.CompilerServices.SkipLocalsInitAttribute", context, null);
        sb.NewLine().Append("static ").Append(cmdType).Append(" ").Append(LocalPrefix).Append("CreateCommand(").Append(connectionType)
            .Append(" connection");
        if (!commandTextAsLiteral) sb.Append(", string? commandText");
        sb.Append(")").Indent();

        if (commandTextAsLiteral && commandType == CommandType.Text && location is not null)
        {
            commandText = "/* " + location + " */ " + commandText;
        }

        // check encryption; this can only be set in the constructor; need to resolve the special constructor
        bool emitCreateCommand = true, setCommandText = true;
        if (method.GetAttributes().SingleOrDefault(x => x.AttributeClass.IsExact("Dapper", "EncryptionAttribute", 0))
            .TryGetAttributeValue("EncryptionKind", out int kind) && kind != 0)
        {
            foreach (var ctor in cmdType.GetMembers(".ctor"))
            {
                if (ctor is IMethodSymbol ctorMethod && ctorMethod.Arity == 0)
                {
                    var p = ctorMethod.Parameters;
                    if (p.Length == 4
                        && p[0].Type.SpecialType == SpecialType.System_String
                        && SymbolEqualityComparer.Default.Equals(p[1].Type, connectionType)
                        && p[2].Type.IsKindOf("System", "Data", "IDbTransaction", 0)
                        && p[3].Type.TypeKind == TypeKind.Enum
                        && p[3].Type.Name == "SqlCommandColumnEncryptionSetting")
                    {
                        sb.NewLine().Append("var command = new ").Append(cmdType).Append(@"(");
                        if (commandTextAsLiteral)
                        {
                            sb.AppendVerbatimLiteral(commandText);
                        }
                        else
                        {
                            sb.Append("commandText"); // use the parameter
                        }
                        sb.Append(", connection, null!, ").AppendEnumLiteral(p[3].Type, kind).Append(");");
                        emitCreateCommand = setCommandText = false;
                        break;
                    }
                }
            }
            if (emitCreateCommand)
            {   // TODO: move to an analyzer output
                sb.NewLine().Append("#error Unable to resolve constructor for encryption configuration");
            }
        }
        if (emitCreateCommand)
        {
            sb.NewLine().Append("var command = connection.CreateCommand();");
        }
        AddProviderSpecificSettings(sb, cmdType, "command", context);
        sb.NewLine().Append("command.CommandType = global::System.Data.CommandType.").Append(commandType.ToString()).Append(";");
        if (setCommandText)
        {
            sb.NewLine().Append("command.CommandText = ");
            if (commandTextAsLiteral)
            {
                sb.AppendVerbatimLiteral(commandText);
            }
            else
            {
                sb.Append("commandText"); // use the parameter
            }
            sb.Append(";");
        }
        index = 0;
        sb.NewLine().Append("var args = command.Parameters;");
        foreach (var p in method.Parameters)
        {
            if (GetHandledType(p.Type) != HandledType.None) continue;
            if (TryGetCommandText(p.GetAttributes(), out _) is not null) continue; // ignore commands
            if (IsDictionary(p.Type, out _)) continue;
            sb.NewLine().NewLine();
            if (index == 0) sb.Append("var ");
            sb.Append("p = ").Append("command").Append(".CreateParameter();");
            sb.NewLine().Append("p.ParameterName = ").AppendVerbatimLiteral(p.Name).Append(";")
                .NewLine().Append("p.Direction = global::System.Data.ParameterDirection.Input;");
            var type = GetDbType(p, out var size);
            if (type is not null)
            {
                sb.NewLine().Append("p.DbType = global::System.Data.DbType.").Append(type.ToString()).Append(";");
            }
            if (size is not null)
            {
                sb.NewLine().Append("p.Size = ").Append(size.GetValueOrDefault()).Append(";");
            }

            sb.NewLine().Append("args.Add(").Append("p);");
            index++;
        }
        sb.NewLine().NewLine().Append("return command;");
        sb.Outdent();
        sb.Outdent();
        return true;

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

        static void WriteExecuteReader(CodeWriter sb, QueryFlags flags, string cancellationToken)
        {
            sb.NewLine().Append("// execute reader");
            sb.NewLine().Append("const global::System.Data.CommandBehavior ").Append(LocalPrefix).Append("behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult");
            if (!flags.Has(QueryFlags.DemandAtMostOneRow))
            {   // if we don't need to check for a second row, can optimize
                sb.Append(" | global::System.Data.CommandBehavior.SingleRow");
            }
            sb.Append(";");

            sb.NewLine().Append(LocalPrefix).Append("reader = ");
            if (flags.IsAsync()) sb.Append("await ");
            sb.Append(LocalPrefix);
            sb.Append("command.ExecuteReader").Append(flags.IsAsync() ? "Async(" : "(").Append(LocalPrefix).Append("close ? (")
                .Append(LocalPrefix).Append("behavior | global::System.Data.CommandBehavior.CloseConnection").Append(") : ").Append(LocalPrefix).Append("behavior");
            if (flags.IsAsync())
            {
                sb.Append(", ").Append(cancellationToken).Append(").ConfigureAwait(false);");
            }
            else
            {
                sb.Append(");");
            }
            sb.NewLine().Append(LocalPrefix).Append("close = false; // performed via CommandBehavior");
            sb.NewLine();
        }
        static void ExecuteReaderSingle(CodeWriter sb, QueryFlags flags, ITypeSymbol itemType, string cancellationToken, MaterializerTracker materializers)
        {
            WriteExecuteReader(sb, flags, cancellationToken);

            sb.NewLine().Append("// process single row");
            sb.NewLine().Append(itemType).Append(" ").Append(LocalPrefix).Append("result;");
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("reader.HasRows && ");
            InvokeReaderMethod(sb, flags, nameof(DbDataReader.Read), cancellationToken).Append(")").Indent();

            if (flags.Has(QueryFlags.UseLegacyMaterializer))
            {
                sb.NewLine().Append(LocalPrefix).Append("result = global::Dapper.SqlMapper.GetRowParser<").Append(itemType).Append(">(")
                    .Append(LocalPrefix).Append("reader).Invoke(").Append(LocalPrefix).Append("reader);");
            }
            else
            {
                sb.NewLine().Append(LocalPrefix).Append("result = ").Append(materializers.Namespace).Append('.').Append(materializers.GetMaterializerName(itemType))
                    .Append(".Instance.Read(").Append(LocalPrefix).Append("reader, ref ").Append(LocalPrefix).Append("tokenBuffer);");
            }

            if (flags.Has(QueryFlags.DemandAtMostOneRow))
            {
                sb.DisableObsolete().NewLine().Append("if (");
                InvokeReaderMethod(sb, flags, nameof(DbDataReader.Read), cancellationToken)
                    .Append(") global::Dapper.Internal.InternalUtilities.ThrowMultiple();").RestoreObsolete();
            }
            sb.Outdent().NewLine().Append("else").Indent();
            if (flags.Has(QueryFlags.DemandAtLeastOneRow))
            {
                sb.DisableObsolete().NewLine().Append("global::Dapper.Internal.InternalUtilities.ThrowNone();").RestoreObsolete();
            }
            sb.NewLine().Append(LocalPrefix).Append("result = default!;").Outdent();
            ConsumeAdditionalResults(sb, flags, cancellationToken);
            sb.NewLine().Append("return ").Append(LocalPrefix).Append("result;");
        }

        static void ConsumeAdditionalResults(CodeWriter sb, QueryFlags flags, string cancellationToken)
        {
            sb.NewLine().Append("// consume additional results (ensures errors from the server are observed)")
                .NewLine().Append("while (");
            InvokeReaderMethod(sb, flags, nameof(DbDataReader.NextResult), cancellationToken).Append(") { }");
        }

        static CodeWriter InvokeReaderMethod(CodeWriter sb, QueryFlags flags, string name, string cancellationToken)
            => flags.IsAsync()
                ? sb.Append("await ").Append(LocalPrefix).Append("reader.").Append(name).Append("Async(").Append(cancellationToken)
                    .Append(").ConfigureAwait(false)")
                : sb.Append(LocalPrefix).Append("reader.").Append(name).Append("()");

        static void ExecuteReaderMultiple(CodeWriter sb, in QueryCategory category, string cancellationToken)
        {
            WriteExecuteReader(sb, category.Flags, cancellationToken);

            sb.NewLine().Append("// process multiple rows");
            if (category.NeedsListLocal() && !category.UseCollector())
            {
                sb.NewLine().Append(LocalPrefix).Append("result = new ").Append(category.ListType).Append("();");
            }
            sb.NewLine().Append("if (").Append(LocalPrefix).Append("reader.HasRows)").Indent();
            if (category.Flags.Has(QueryFlags.UseLegacyMaterializer))
            {
                sb.NewLine().Append("var ").Append(LocalPrefix).Append("parser = global::Dapper.SqlMapper.GetRowParser<").Append(category.ItemType).Append(">(")
                .Append(LocalPrefix).Append("reader);");
            }
            else
            {
                sb.NewLine().Append("var ").Append(LocalPrefix).Append("parser = global::Dapper.TypeReader.TryGetReader<").Append(category.ItemType).Append(">()!;");

                if (category.IsAsync())
                {
                    sb.NewLine().Append("var ").Append(LocalPrefix).Append("tokens = global::Dapper.TypeReader.RentSegment(ref ").Append(LocalPrefix).Append("tokenBuffer, ").Append(LocalPrefix).Append("reader.FieldCount);");
                }
                else
                {
                    sb.NewLine().Append("global::System.Span<int> ").Append(LocalPrefix).Append("tokens = ").Append(LocalPrefix).Append("reader.FieldCount <= global::Dapper.TypeReader.MaxStackTokens ? stackalloc int[")
                        .Append(LocalPrefix).Append("reader.FieldCount] : global::Dapper.TypeReader.RentSpan(ref ").Append(LocalPrefix).Append("tokenBuffer, ").Append(LocalPrefix).Append("reader.FieldCount);");
                }
                sb.NewLine().Append(LocalPrefix).Append("parser.IdentifyFieldTokensFromSchema(").Append(LocalPrefix).Append("reader, ").Append(LocalPrefix).Append("tokens);");
            }
            sb.NewLine().Append("while (");
            InvokeReaderMethod(sb, category.Flags, nameof(DbDataReader.Read), cancellationToken).Append(")").Indent();

            // open the yield/Add line
            if (category.ListStrategy == ListStrategy.Yield)
            {
                sb.NewLine().Append("yield return ");
            }
            else
            {
                sb.NewLine().Append(LocalPrefix).Append("result.Add(");
            }
            // materialize an item
            if (category.Flags.Has(QueryFlags.UseLegacyMaterializer))
            {
                sb.Append(LocalPrefix).Append("parser(").Append(LocalPrefix).Append("reader)");
            }
            else if (category.IsAsync())
            {
                sb.Append("await ").Append(LocalPrefix).Append("parser.ReadAsync(").Append(LocalPrefix).Append("reader, ").Append(LocalPrefix).Append("tokens, ").Append(cancellationToken).Append(").ConfigureAwait(false)");
            }
            else
            {
                sb.Append(LocalPrefix).Append("parser.Read(").Append(LocalPrefix).Append("reader, ").Append(LocalPrefix).Append("tokens)");
            }
            // close the yield/Add line
            sb.Append(category.ListStrategy == ListStrategy.Yield ? ";" : ");");
            
            sb.Outdent().Outdent();
            ConsumeAdditionalResults(sb, category.Flags, cancellationToken);
        }

        static void ExecuteNonQuery(CodeWriter sb, QueryFlags flags, string cancellationToken)
        {
            sb.NewLine().Append("// execute non-query").NewLine();
            if (flags.IsAsync())
            {

                sb.Append("await ").Append(LocalPrefix).Append("command.ExecuteNonQueryAsync(").Append(cancellationToken).Append(").ConfigureAwait(false);");
                // .Append(").ConfigureAwait(false);");
            }
            else
            {
                sb.Append(LocalPrefix).Append("command.ExecuteNonQuery();");
            }
        }
    }

    static void AddProviderSpecificSettings(CodeWriter sb, ITypeSymbol cmdType, string target, in GeneratorExecutionContext context)
    {
        // TODO: generalize; add SqlCommand.EnableOptimizedParameterBinding
        static void TestForKnownCommand(CodeWriter sb, ITypeSymbol cmdType, string target, in GeneratorExecutionContext context, string fullyQualifiedName, ref int index)
        {
            var foundType = context.Compilation.GetTypeByMetadataName(fullyQualifiedName);
            if (foundType is not null)
            {
                if (!SymbolEqualityComparer.Default.Equals(cmdType, foundType))
                {
                    // perform a type test
                    var length = sb.Length; // so we can rollback if we fail to find things that *should* be there
                    sb.NewLine();
                    if (index != 0) sb.Append("else ");
                    sb.Append("if (").Append(target).Append(" is ").Append(foundType).Append(" typed").Append(index).Append(")").Indent();
                    var typedTarget = "typed" + index.ToString(CultureInfo.InvariantCulture);
                    bool bbn = TrySetProperty(sb, foundType, "BindByName", SpecialType.System_Boolean, typedTarget, "true");
                    bool ilfs = TrySetProperty(sb, foundType, "InitialLONGFetchSize", SpecialType.System_Int32, typedTarget, "-1");
                    sb.Outdent();
                    if (bbn | ilfs)
                    {
                        index++;
                    }
                    else
                    {   // revert
                        sb.Length = length;
                    }
                }
            }
        }

        static bool TrySetProperty(CodeWriter sb, ITypeSymbol cmdType, string propertyName, SpecialType expectedType, string target, string value)
        {
            var props = cmdType.GetMembers(propertyName);
            if (props.Length == 1 && props[0] is IPropertySymbol prop && prop.Type.SpecialType == expectedType)
            {
                sb.NewLine().Append(target).Append('.').Append(propertyName).Append(" = ").Append(value).Append(';');
                return true;
            }
            return false;
        }

        // check for special properties, bound by signature
        bool needBBN = !TrySetProperty(sb, cmdType, "BindByName", SpecialType.System_Boolean, target, "true");
        bool needILFS = !TrySetProperty(sb, cmdType, "InitialLONGFetchSize", SpecialType.System_Int32, target, "-1");

        if ((needBBN | needILFS) && (cmdType.TypeKind == TypeKind.Interface || cmdType.IsAbstract))
        {
            int typedIndex = 0;
            TestForKnownCommand(sb, cmdType, target, context, "Oracle.DataAccess.Client.OracleCommand", ref typedIndex);
            TestForKnownCommand(sb, cmdType, target, context, "Oracle.ManagedDataAccess.Client.OracleCommand", ref typedIndex);
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
                attrib.TryGetAttributeValue<string>("CommandText", out var commandText);
                if (commandText is null) commandText = "";

                if (attrib.TryGetAttributeValue<int>("CommandType", out var rawValue))
                {
                    commandType = (CommandType)rawValue;
                }
                else if (string.IsNullOrWhiteSpace(commandText))
                {
                    commandType = CommandType.Text;
                }
                else
                {
                    commandType = commandText.IndexOf(' ') >= 0 ? CommandType.Text : CommandType.StoredProcedure;
                }
                return commandText;
            }
        }
        commandType = default;
        return null;
    }

    internal static bool IsCommandAttribute(ITypeSymbol? type)
        => type.IsExact("Dapper", "CommandAttribute", 0);
}

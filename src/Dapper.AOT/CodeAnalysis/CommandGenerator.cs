using Dapper.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Dapper.Internal.Utilities;

namespace Dapper.CodeAnalysis
{
	/// <summary>
	/// Parses the source for methods annotated with <c>Dapper.CommandAttribute</c>, generating an appropriate implementation.
	/// </summary>
	[Generator]
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
        public event Action<string>? Log;

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

                    Log?.Invoke($"Detected candidate: '{method.Name}'");
                    (candidates ??= new()).Add((GetNamespace(method.ContainingType), GetTypeName(method.ContainingType), method, syntaxNode));
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
            var type = GetType();
            var sb = CodeWriter.Create().Append("#nullable enable")
                .NewLine().Append("//------------------------------------------------------------------------------")
                .NewLine().Append("// <auto-generated>")
                .NewLine().Append("// This code was generated by:")
                .NewLine().Append("//     ").Append(type.FullName).Append(" v").Append(ReportVersion ? GetVersion(type.Assembly) : "N/A")
                .NewLine().Append("// Changes to this file may cause incorrect behavior and")
                .NewLine().Append("// will be lost if the code is regenerated.")
                .NewLine().Append("// </auto-generated>")
                .NewLine().Append("//------------------------------------------------------------------------------")
                .NewLine().Append("#region Designer generated code");

            int totalWritten = 0;
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
                        if (WriteMethod(candidate.Method, candidate.Syntax, sb, materializers, context))
                            totalWritten++;
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

            if (totalWritten == 0) sb.Length = 0;
            else sb.NewLine().Append("#endregion");
            return sb.ToStringRecycle();
        }

        private bool WriteMethod(IMethodSymbol method, MethodDeclarationSyntax syntax, CodeWriter sb, Dictionary<ITypeSymbol, string> materializers, in GeneratorExecutionContext context)
        {
            var attribs = method.GetAttributes();
            var text = TryGetCommandText(attribs, out var commandType);
            if (text is null)
            {
                Log?.Invoke($"No command-text resolved for '{method.Name}'");
                return false;
            }

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
                            Log?.Invoke($"Multiple connection accessors found for '{method.Name}'");
                            return false;
                        }
                        connection = p.Name;
                        connectionType = p.Type;
                        break;
                    case HandledType.Transaction:
                        if (transaction is not null)
                        {
                            Log?.Invoke($"Multiple transaction accessors found for '{method.Name}'");
                            return false;
                        }
                        transaction = p.Name;
                        transactionType = p.Type;
                        break;
                    case HandledType.CancellationToken:
                        if (cancellationToken is not null)
						{
                            Log?.Invoke($"Multiple cancellation tokens found for '{method.Name}'");
                            return false;
                        }
                        cancellationToken = p.Name;
                        break;
                }
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
                return WriteMethodViaAdoDotNet(method, syntax, sb, materializers, connection, connectionType!, transaction, transactionType!, text, commandType, context, cancellationToken);
            }
            // TODO: other APIs here
            else
            {
                Log?.Invoke($"No connection accessors found for '{method.Name}'");
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
                    if (type.IsExact("System", "Data", "IDbConnection") || type.IsKindOf("System", "Data", "Common", "DbConnection"))
                        return HandledType.Connection;
                    if (type.IsExact("System", "Data", "IDbTransaction") || type.IsKindOf("System", "Data", "Common", "DbTransaction"))
                        return HandledType.Transaction;
                }
                else
                {
                    if (type.IsExact("System", "Threading", "CancellationToken"))
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
                        return method.ContainingType.Name + "." + method.Name + ", " + sp.Path + " #" + line;
                    }
                }
            }
            identifier = null;
            return null;
        }

        static DbType? GetDbType(IParameterSymbol parameter, out int? size)
        {
            size = default;
            var type = parameter.Type;
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
                if (type.IsExact("System", "DateTimeOffset"))
                    dbType = DbType.DateTimeOffset;
                else if (type.IsExact("System", "Guid"))
                    dbType = DbType.Guid;
            }

            if (dbType == DbType.String)
            {
                size = -1;
            }
            return default;
        }

        static void AddIfMissing(CodeWriter sb, string attribute, in GeneratorExecutionContext context, IMethodSymbol? method)
		{
            var found = context.Compilation.GetTypeByMetadataName(attribute);
            if (found is not null && (method is null || !method.IsDefined(found)))
			{
                sb.NewLine().Append("[").Append(found).Append("]");
            }
        }
        bool WriteMethodViaAdoDotNet(IMethodSymbol method, MethodDeclarationSyntax syntax, CodeWriter sb, Dictionary<ITypeSymbol, string> materializers, string connection, ITypeSymbol connectionType, string? transaction, ITypeSymbol transactionType, string commandText, CommandType commandType, in GeneratorExecutionContext context, string cancellationToken)
        {
            const string LocalPrefix = "__dapper__";

            bool allowUnsafe = context.Compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;

            var cmdType = GetMethodReturnType(connectionType, nameof(IDbConnection.CreateCommand));
            if (cmdType is null)
            {
                Log?.Invoke($"Unable to resolve command-type for '{method.Name}'");
                return false;
            }
            (var flags, var itemType, var itemNullability) = CategorizeQuery(method);
            ITypeSymbol? readerType = null;
            if (flags.Has(QueryFlags.IsQuery))
            {
                readerType = GetMethodReturnType(cmdType, nameof(IDbCommand.ExecuteReader));
                if (readerType is null)
                {
                    Log?.Invoke($"Unable to resolve reader-type for '{method.Name}'");
                    return false;
                }
            }

            var location = GetLocation(method, out string? identifier);
            var commandField = identifier is null ? null : ("s_" + LocalPrefix + "command_" + identifier);

            if (commandField is not null)
            {
                sb.NewLine().NewLine().Append("private static ").Append(cmdType).Append("? ").Append(commandField).Append(";").NewLine();
            }

            AddIfMissing(sb, "System.Diagnostics.DebuggerNonUserCodeAttribute", context, method);
            if (allowUnsafe) AddIfMissing(sb, "System.Runtime.CompilerServices.SkipLocalsInitAttribute", context, method);
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
            if (flags.Has(QueryFlags.IsAsync)) sb.Append(" async");

            sb.Append(" partial ").Append(method.ReturnType).Append(
                method.ReturnType.IsReferenceType && method.ReturnNullableAnnotation == NullableAnnotation.Annotated ? "? " : " ").Append(method.Name).Append("(");
            bool first = true;
            // Log?.Invoke(TypeMetadata.Get(method).ToString());
            foreach (var p in method.Parameters)
            {
                if (first)
                {
                    if (method.IsExtensionMethod) sb.Append("this ");
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(p.Type).Append(p.NullableAnnotation == NullableAnnotation.Annotated ? "? " : " ").Append(p.Name);

                //Log?.Invoke($"{p.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} => {TypeMetadata.Get(p)}");
            }
            sb.Append(")");
            sb.Indent();

            // variable declarations
            sb.NewLine().Append("// locals");
            sb.NewLine().Append(cmdType).Append("? ").Append(LocalPrefix).Append("command = null;");
            if (readerType is not null) sb.NewLine().Append(readerType).Append("? ").Append(LocalPrefix).Append("reader = null;");
            sb.NewLine().Append("bool ").Append(LocalPrefix).Append("close = false;");
            bool useFieldNumbers = false; // TODO: come back
            if (useFieldNumbers)
            {
                sb.NewLine().Append("int[]? ").Append(LocalPrefix).Append("fieldNumbers = null;");
            }
            sb.NewLine().Append("try");
            sb.Indent();

            // prepare connection
            sb.NewLine().Append("// prepare connection");
            sb.NewLine().Append("if (").Append(connection).Append("!.State == global::System.Data.ConnectionState.Closed)").Indent();
            if (flags.Has(QueryFlags.IsAsync))
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
            sb.NewLine().Append(LocalPrefix).Append("command = ").Append(LocalPrefix).Append("CreateCommand(").Append(connection).Append("!);");
            if (commandField is not null)
            {
                sb.Outdent().NewLine().Append("else").Indent().NewLine().Append(LocalPrefix).Append("command.Connection = ").Append(connection).Append(";").Outdent();
            }

            // assign parameter values
            int index = 0;
            foreach (var p in method.Parameters)
            {
                if (GetHandledType(p.Type) != HandledType.None) continue;
                if (index == 0)
                {
                    sb.NewLine().NewLine().Append("// assign parameter values");
                    sb.DisableObsolete();
                }
                sb.NewLine().Append(LocalPrefix).Append("command.Parameters[").Append(index++).Append("].Value = global::Dapper.Internal.InternalUtilities.AsValue(").Append(p.Name).Append(");");
            }
            if (index != 0) sb.RestoreObsolete();

            // execute
            sb.NewLine().NewLine().Append("// execute");
            if (flags.Has(QueryFlags.IsQuery) && itemType is not null)
            {
                if (flags.Has(QueryFlags.IsSingle))
                {
                    ExecuteReaderSingle(sb, flags, itemType);
                }
                else
                {
                    // TODO: other query kinds
                    Log?.Invoke($"Query not supported: {flags} via '{method.Name}'");
                    return false;
                }
            }
            else if (flags.Has(QueryFlags.IsScalar) && itemType is not null)
            {
                // TODO: scalars
                Log?.Invoke($"Scalar not supported: {flags} via '{method.Name}'");
                return false;
            }
            else
            {
                // non-query
                ExecuteNonQuery(sb, flags, cancellationToken);
            }

            sb.Outdent();
            sb.NewLine().Append("finally");
            // cleanup code
            sb.Indent();
            sb.NewLine().Append("// cleanup");
            if (useFieldNumbers)
            {
                sb.NewLine().Append("if (").Append(LocalPrefix).Append("fieldNumbers is not null) global::System.Buffers.ArrayPool<int>.Shared.Return(").Append(LocalPrefix).Append("fieldNumbers);");
            }
            if (readerType is not null)
            {
                if (flags.Has(QueryFlags.IsAsync) && readerType.HasDisposeAsync())
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
            if (flags.Has(QueryFlags.IsAsync) && cmdType.HasDisposeAsync())
            {
                sb.NewLine().Append("if (").Append(LocalPrefix).Append("command is not null) ").Append("await ").Append(LocalPrefix).Append("command.DisposeAsync().ConfigureAwait(false);");
            }
            else
            {
                sb.NewLine().Append(LocalPrefix).Append("command?.Dispose();");
            }
            sb.Outdent();

            if (flags.Has(QueryFlags.IsAsync) && connectionType.HasBasicAsyncMethod("CloseAsync", out var awaitableType))
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

            // command initialization
            sb.NewLine().NewLine().Append("// command factory for ").Append(method.Name);
            AddIfMissing(sb, "System.Diagnostics.DebuggerNonUserCodeAttribute", context, method);
            if (allowUnsafe) AddIfMissing(sb, "System.Runtime.CompilerServices.SkipLocalsInitAttribute", context, null);
            sb.NewLine().Append("static ").Append(cmdType).Append(" ").Append(LocalPrefix).Append("CreateCommand(").Append(connectionType)
                .Append(" connection)").Indent();
            sb.NewLine().Append("var command = connection.CreateCommand();");
            AddProviderSpecificSettings(sb, cmdType, "command", context);
            sb.NewLine().Append("command.CommandType = global::System.Data.CommandType.").Append(commandType.ToString()).Append(";");


            if (commandType == CommandType.Text && location is not null)
            {
                commandText = "/* " + location + " */ " + commandText;
            }

            sb.NewLine().Append("command.CommandText = ").AppendVerbatimLiteral(commandText).Append(";");
            index = 0;
            sb.NewLine().Append("var args = command.Parameters;");
            foreach (var p in method.Parameters)
            {
                if (GetHandledType(p.Type) != HandledType.None) continue;
                sb.NewLine().NewLine();
                if (index == 0) sb.Append("var ");
                sb.Append("p = ").Append("command").Append(".CreateParameter();");
                sb.NewLine().Append("p.ParameterName = ").AppendVerbatimLiteral(p.Name).Append(";")
                    .NewLine().Append("p.Direction = global::System.Data.ParameterDirection.Input;");
                var type = GetDbType(p, out var size);
                if (type is not null)
                {
                    sb.NewLine().Append("p.DbType = global::System.Data.DbType").Append(type.ToString()).Append(";");
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

            static void ExecuteReaderSingle(CodeWriter sb, QueryFlags flags, ITypeSymbol itemType)
			{
                sb.NewLine().Append("const global::System.Data.CommandBehavior ").Append(LocalPrefix).Append("behavior = global::System.Data.CommandBehavior.SequentialAccess | global::System.Data.CommandBehavior.SingleResult");
                if (!flags.Has(QueryFlags.DemandAtMostOneRow))
                {   // if we don't need to check for a second row, can optimize
                    sb.Append(" | global::System.Data.CommandBehavior.SingleRow");
                }
                sb.Append(";");

                sb.NewLine().Append(LocalPrefix).Append("reader = ").Append(LocalPrefix).Append("command.ExecuteReader(").Append(LocalPrefix).Append("close ? (")
                    .Append(LocalPrefix).Append("behavior | global::System.Data.CommandBehavior.CloseConnection").Append(") : ").Append(LocalPrefix).Append("behavior);");
                sb.NewLine().Append(LocalPrefix).Append("close = false; // performed via CommandBehavior");
                sb.NewLine();
                sb.NewLine().Append(itemType).Append(" ").Append(LocalPrefix).Append("result;");
                sb.NewLine().Append("if (").Append(LocalPrefix).Append("reader.HasRows && ").Append(LocalPrefix).Append("reader.Read())").Indent();

                sb.NewLine().Append(LocalPrefix).Append("result = global::Dapper.SqlMapper.GetRowParser<").Append(itemType).Append(">(")
                    .Append(LocalPrefix).Append("reader).Invoke(").Append(LocalPrefix).Append("reader);");

                if (flags.Has(QueryFlags.DemandAtMostOneRow))
                {
                    sb.DisableObsolete().NewLine().Append("if (").Append(LocalPrefix).Append("reader.Read() global::Dapper.Internal.InternalUtilities.ThrowMultiple();").RestoreObsolete();
                }
                sb.Outdent().NewLine().Append("else").Indent();
                if (flags.Has(QueryFlags.DemandAtLeastOneRow))
                {
                    sb.DisableObsolete().NewLine().Append("global::Dapper.Internal.InternalUtilities.ThrowNone();").RestoreObsolete();
                }
                sb.NewLine().Append(LocalPrefix).Append("result = default!;");
                sb.Outdent().NewLine().Append("while (").Append(LocalPrefix).Append("reader.NextResult()) { } // consumes TDS to check for exceptions");
                sb.NewLine().Append("return ").Append(LocalPrefix).Append("result;");
            }

            static void ExecuteNonQuery(CodeWriter sb, QueryFlags flags, string cancellationToken)
			{
                sb.NewLine();
                if (flags.Has(QueryFlags.IsAsync))
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
                    if (attrib.TryGetAttributeValue<string>("CommandText", out var commandText))
                    {
                        if (attrib.TryGetAttributeValue<int>("CommandType", out var rawValue))
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

        internal static bool IsCommandAttribute(ITypeSymbol? type)
            => type.IsExact("Dapper", "CommandAttribute");
    }
}

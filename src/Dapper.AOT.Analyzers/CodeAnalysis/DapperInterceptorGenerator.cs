using Dapper.CodeAnalysis.Abstractions;
using Dapper.CodeAnalysis.Writers;
using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using static Dapper.Internal.Inspection;

namespace Dapper.CodeAnalysis;

[Generator(LanguageNames.CSharp), DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class DapperInterceptorGenerator : InterceptorGeneratorBase
{
    private readonly bool _withInterceptionRecording = false; 
    
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticsBase.All<Diagnostics>();

#pragma warning disable CS0067 // unused; retaining for now
    public event Action<string>? Log;
#pragma warning restore CS0067

    /// <summary>
    /// Creates an interceptor generator for Dapper
    /// </summary>
    public DapperInterceptorGenerator()
    {
    }

    /// <summary>
    /// Creates an interceptor generator for Dapper used for Tests.
    /// </summary>
    /// <note>
    /// It will insert very specific call with known method name.
    /// Users will not have a reference to inserted assembly code, therefore: don't make it public 
    /// </note>
    internal DapperInterceptorGenerator(bool withInterceptionRecording)
    {
        _withInterceptionRecording = withInterceptionRecording;
    }
    
    public override void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                    .Where(x => x is not null)
                    .Select((x, _) => x!);
        var combined = context.CompilationProvider.Combine(nodes.Collect());
        context.RegisterImplementationSourceOutput(combined, Generate);
    }

    // very fast and light-weight; we'll worry about the rest later from the semantic tree
    internal static bool IsCandidate(string methodName) =>
        methodName.StartsWith("Execute")
        || methodName.StartsWith("Query")
        || methodName.StartsWith("GetRowParser");

    internal bool PreFilter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            return IsCandidate(ma.Name.ToString());
        }

        return false;
    }

    private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        try
        {
            return Parse(new(ctx, cancellationToken));
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
            return null;
        }
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Chosen API")]
    internal SourceState? Parse(ParseState ctx)
    {
        try
        {
            if (ctx.Node is not InvocationExpressionSyntax ie
                || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op
                || !op.IsDapperMethod(out var flags)
                || flags.HasAny(OperationFlags.NotAotSupported | OperationFlags.DoNotGenerate)
                || !Inspection.IsEnabled(ctx, op, Types.DapperAotAttribute, out var aotAttribExists))
            {
                return null;
            }

            var location = DapperAnalyzer.SharedParseArgsAndFlags(ctx, op, ref flags, out var sql, out var argExpression, reportDiagnostic: null, out var resultType, exitFirstFailure: true);
            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                return null;
            }



            // additional result-type checks

            // perform SQL inspection
            var map = MemberMap.CreateForParameters(argExpression);
            var parameterMap = BuildParameterMap(ctx, op, sql, ref flags, map, location, out var parseFlags);

            if (flags.HasAny(OperationFlags.CacheCommand))
            {
                bool canBeCached = true;
                // need fixed text, command-type and parameters to be reusable
                if (string.IsNullOrWhiteSpace(sql) || parameterMap == "?" || !flags.HasAny(OperationFlags.StoredProcedure | OperationFlags.TableDirect | OperationFlags.Text))
                {
                    canBeCached = false;
                }

                if (!canBeCached) flags &= ~OperationFlags.CacheCommand;
            }

            var additionalState = AdditionalCommandState.Parse(Inspection.GetSymbol(ctx, op), map, null);

            Debug.Assert(!flags.HasAny(OperationFlags.DoNotGenerate), "should have already exited");
            return new SuccessSourceState(location, op.TargetMethod, flags, sql, resultType, argExpression?.Type, parameterMap, additionalState);
        }
        catch (Exception ex)
        {
            Location? loc = null;
            try
            {
                loc = ctx.Node.GetLocation();
            }
            catch { } // best effort only
            return new FaultSourceState(loc, ex);
        }

        static string BuildParameterMap(in ParseState ctx, IInvocationOperation op, string? sql, ref OperationFlags flags, MemberMap? map, Location loc, out SqlParseOutputFlags parseFlags)
        {
            // check the arg type
            var args = DapperAnalyzer.SharedGetParametersToInclude(map, ref flags, sql, null, out parseFlags);
            if (args is null) return "?"; // deferred
            var arr = args.Value;

            if (arr.IsDefaultOrEmpty) return ""; // nothing to add

            switch (arr.Length)
            {
                case 0: return "";
                case 1: return arr[0].CodeName;
                case 2: return arr[0].CodeName + " " + arr[1].CodeName;
            }
            var sb = new StringBuilder();
            foreach (var arg in arr)
            {
                if (sb.Length != 0) sb.Append(' ');
                sb.Append(arg.CodeName);
            }
            return sb.ToString();
        }
    }


    private static string? GetCommandFactory(Compilation compilation, out bool canConstruct)
    {
        foreach (var attribute in compilation.SourceModule.GetAttributes())
        {
            if (attribute.AttributeClass is
                {
                    Name: "CommandFactoryAttribute", Arity: 1, ContainingNamespace:
                    {
                        Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true
                    }
                })
            {
                var type = attribute.AttributeClass.TypeArguments[0];
                canConstruct = false;
                // need non-abstract and public parameterless constructor
                if (!type.IsAbstract && type is INamedTypeSymbol named && named.Arity == 1)
                {
                    foreach (var ctor in named.InstanceConstructors)
                    {
                        if (ctor.Parameters.IsEmpty)
                        {
                            canConstruct = ctor.DeclaredAccessibility == Accessibility.Public;
                            break;
                        }
                    }
                }
                var name = CodeWriter.GetTypeName(type);
                var trimGeneric = name.LastIndexOf('<');
                if (trimGeneric >= 0)
                {
                    name = name.Substring(0, trimGeneric);
                }
                return name;
            }
        }
        canConstruct = true; // we mean the default Dapper one, which can be constructed
        return null;
    }

    internal static class FeatureKeys
    {
        public const string InterceptorsPreviewNamespaces = nameof(InterceptorsPreviewNamespaces),
            CodegenNamespace = "Dapper.AOT";
        public static KeyValuePair<string, string> InterceptorsPreviewNamespacePair => new(InterceptorsPreviewNamespaces, CodegenNamespace);
    }

    private static bool CheckPrerequisites(in GenerateState ctx)
    {
        if (ctx.Nodes.IsDefaultOrEmpty) return false; // nothing to do

        // find the first enabled thing with a C# parse options
        if (ctx.Nodes.OfType<SuccessSourceState>().FirstOrDefault()?.Location?.SourceTree?.Options is not CSharpParseOptions options) return false; // not C#

        bool success = true;

        var version = options.LanguageVersion;
        if (version != LanguageVersion.Default && version < LanguageVersion.CSharp11)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.LanguageVersionTooLow, null));
            success = false;
        }
        return success;
        
    }

    private void Generate(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        try
        {
            Generate(new(ctx, state));
        }
        catch (Exception ex)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticsBase.UnknownError, null, ex.Message, ex.StackTrace));
        }
    }

    const string DapperBaseCommandFactory = "global::Dapper.CommandFactory";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Allow expectation of state")]
    internal void Generate(in GenerateState ctx)
    {
        foreach (var fault in ctx.Nodes.OfType<FaultSourceState>())
        {
            var ex = fault.Fault;
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownError, fault.Location, ex.Message, ex.StackTrace));
        }

        if (!CheckPrerequisites(ctx)) // also reports per-item diagnostics
        {
            // failed checks; do nothing
            return;
        }

        var dbCommandTypes = IdentifyDbCommandTypes(ctx.Compilation, out var needsCommandPrep);

        bool allowUnsafe = ctx.Compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;
        var sb = new CodeWriter().Append("#nullable enable").NewLine()
            .Append("namespace ").Append(FeatureKeys.CodegenNamespace).Append(" // interceptors must be in a known namespace").Indent().NewLine()
            .Append("file static class DapperGeneratedInterceptors").Indent().NewLine();
        int methodIndex = 0, callSiteCount = 0;

        var factories = new CommandFactoryState(ctx.Compilation);
        var readers = new RowReaderState();

        foreach (var grp in ctx.Nodes.OfType<SuccessSourceState>().Where(x => !x.Flags.HasAny(OperationFlags.DoNotGenerate)).GroupBy(x => x.Group(), CommonComparer.Instance))
        {
            // first, try to resolve the helper method that we're going to use for this
            var (flags, method, parameterType, parameterMap, _, additionalCommandState) = grp.Key;
            const bool useUnsafe = false;
            int usageCount = 0;

            foreach (var op in grp.OrderBy(row => row.Location, CommonComparer.Instance))
            {
                var loc = op.Location.GetLineSpan();
                var start = loc.StartLinePosition;
                sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                    .AppendVerbatimLiteral(ctx.GetInterceptorFilePath(op.Location.SourceTree)).Append(", ").Append(start.Line + 1).Append(", ").Append(start.Character + 1).Append(")]").NewLine();
                usageCount++;
            }



            if (usageCount == 0)
            {
                continue; // empty group?
            }
            callSiteCount += usageCount;

            // declare the method
            sb.Append("internal static ").Append(useUnsafe ? "unsafe " : "")
                .Append(method.ReturnType)
                .Append(" ").Append(method.Name).Append(methodIndex++).Append("(");
            var parameters = method.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                else if (method.IsExtensionMethod) sb.Append("this ");
                sb.Append(parameters[i].Type).Append(" ").Append(parameters[i].Name);
            }
            sb.Append(")").Indent().NewLine();
            sb.Append("// ").Append(flags.ToString()).NewLine();
            if (flags.HasAny(OperationFlags.HasParameters))
            {
                sb.Append("// takes parameter: ").Append(parameterType).NewLine();
            }
            if (!string.IsNullOrWhiteSpace(grp.Key.ParameterMap))
            {
                sb.Append("// parameter map: ").Append(grp.Key.ParameterMap switch
                {
                    "?" => "(deferred)",
                    "*" => "(everything)",
                    _ => grp.Key.ParameterMap,
                }).NewLine();
            }
            ITypeSymbol? resultType = null;
            if (flags.HasAny(OperationFlags.TypedResult))
            {
                resultType = grp.First().ResultType!;
                sb.Append("// returns data: ").Append(resultType).NewLine();
            }

            // assertions
            var commandTypeMode = flags & (OperationFlags.Text | OperationFlags.StoredProcedure | OperationFlags.TableDirect);
            var methodParameters = grp.Key.Method.Parameters;
            string? fixedSql = null;

            if (HasParam(methodParameters, "sql"))
            {
                if (flags.HasAny(OperationFlags.IncludeLocation))
                {
                    var origin = grp.Single();
                    fixedSql = origin.Sql; // expect exactly one SQL
                    sb.Append("global::System.Diagnostics.Debug.Assert(sql == ")
                        .AppendVerbatimLiteral(fixedSql).Append(");").NewLine();
                    var path = origin.Location.GetMappedLineSpan();
                    fixedSql = $"-- {path.Path}#{path.StartLinePosition.Line + 1}\r\n{fixedSql}";
                }
                else
                {
                    sb.Append("global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));").NewLine();
                }
            }
            if (HasParam(methodParameters, "commandType"))
            {
                if (commandTypeMode != 0)
                {
                    sb.Append("global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.")
                            .Append(commandTypeMode.ToString()).Append(");").NewLine();
                }
            }

            if (flags.HasAny(OperationFlags.Buffered | OperationFlags.Unbuffered) && HasParam(methodParameters, "buffered"))
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(buffered is ").Append((flags & OperationFlags.Buffered) != 0).Append(");").NewLine();
            }

            if (HasParam(methodParameters, "param"))
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(param is ").Append(flags.HasAny(OperationFlags.HasParameters) ? "not " : "").Append("null);").NewLine();
            }

            if (HasParam(methodParameters, "concreteType"))
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(concreteType is null);").NewLine();
            }

            sb.NewLine();

            if (_withInterceptionRecording)
            {
                sb.Append("// record interception for tests assertions").NewLine();
                sb.Append("global::Dapper.AOT.Test.Integration.Executables.Recording.InterceptorRecorderResolver.Resolve().Record();").NewLine();
                sb.NewLine();
            }

            if (flags.HasAny(OperationFlags.GetRowParser))
            {
                WriteGetRowParser(sb, resultType, readers);
            }
            else if (!TryWriteMultiExecImplementation(sb, flags, commandTypeMode, parameterType, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories, fixedSql, additionalCommandState))
            {
                WriteSingleImplementation(sb, method, resultType, flags, commandTypeMode, parameterType, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories, readers, fixedSql, additionalCommandState);
            }

            sb.Outdent().NewLine().NewLine();
        }

        var baseCommandFactory = GetCommandFactory(ctx.Compilation, out var canConstruct) ?? DapperBaseCommandFactory;
        if (needsCommandPrep || !canConstruct)
        {
            // at least one command-type needs special handling; do that
            sb.Append("private class CommonCommandFactory<T> : ").Append(baseCommandFactory).Append("<T>").Indent().NewLine();
            if (needsCommandPrep)
            {
                sb.Append("public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)").Indent().NewLine()
                .Append("var cmd = base.GetCommand(connection, sql, commandType, args);");
                int cmdTypeIndex = 0;
                foreach (var type in dbCommandTypes)
                {
                    var flags = GetSpecialCommandFlags(type);
                    if (flags != SpecialCommandFlags.None)
                    {
                        sb.NewLine().Append("// apply special per-provider command initialization logic for ").Append(type.Name).NewLine()
                            .Append(cmdTypeIndex == 0 ? "" : "else ").Append("if (cmd is ").Append(type).Append(" cmd").Append(cmdTypeIndex).Append(")").Indent().NewLine();
                        if ((flags & SpecialCommandFlags.BindByName) != 0)
                        {
                            sb.Append("cmd").Append(cmdTypeIndex).Append(".BindByName = true;").NewLine();
                        }
                        if ((flags & SpecialCommandFlags.InitialLONGFetchSize) != 0)
                        {
                            sb.Append("cmd").Append(cmdTypeIndex).Append(".InitialLONGFetchSize = -1;").NewLine();
                        }
                        sb.Outdent().NewLine();
                        cmdTypeIndex++;
                    }
                }
                sb.Append("return cmd;").Outdent().NewLine();
            }
            sb.Outdent().NewLine();
            baseCommandFactory = "CommonCommandFactory";
        }

        // add in DefaultCommandFactory as a short-hand to a non-null basic factory
        sb.NewLine();
        if (baseCommandFactory == DapperBaseCommandFactory)
        {
            sb.Append("private static ").Append(baseCommandFactory).Append("<object?> DefaultCommandFactory => ")
                .Append(baseCommandFactory).Append(".Simple;").NewLine();
        }
        else
        {
            sb.Append("private static readonly ").Append(baseCommandFactory).Append("<object?> DefaultCommandFactory = new();").NewLine();
        }
        sb.NewLine();

        foreach (var pair in readers)
        {
            WriteRowFactory(ctx, sb, pair.Type, pair.Index);
        }

        foreach (var tuple in factories)
        {
            WriteCommandFactory(ctx, baseCommandFactory, sb, tuple.Type, tuple.Index, tuple.Map, tuple.CacheCount, tuple.AdditionalCommandState);
        }

        sb.Outdent().Outdent(); // ends our generated file-scoped class and the namespace
        
        var preGeneratedCodeWriter = new PreGeneratedCodeWriter(sb, ctx.Compilation);
        preGeneratedCodeWriter.Write(ctx.GeneratorContext.IncludedGenerationTypes);

        ctx.AddSource((ctx.Compilation.AssemblyName ?? "package") + ".generated.cs", sb.ToString());
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InterceptorsGenerated, null, callSiteCount, ctx.Nodes.Length, methodIndex, factories.Count(), readers.Count()));
    }

    private static void WriteGetRowParser(CodeWriter sb, ITypeSymbol? resultType, in RowReaderState readers)
    {
        sb.Append("return ").AppendReader(resultType, readers)
            .Append(".GetRowParser(reader, startIndex, length, returnNullIfFirstMissing);").NewLine();
    }

    private static void WriteCommandFactory(in GenerateState ctx, string baseFactory, CodeWriter sb, ITypeSymbol type, int index, string map, int cacheCount, AdditionalCommandState? additionalCommandState)
    {
        var declaredType = type.IsAnonymousType ? "object?" : CodeWriter.GetTypeName(type);
        sb.Append("private ").Append(cacheCount <= 1 ? "sealed" : "abstract").Append(" class CommandFactory").Append(index).Append(" : ")
            .Append(baseFactory).Append("<").Append(declaredType).Append(">");
        if (type.IsAnonymousType)
        {
            sb.Append(" // ").Append(type); // give the reader a clue
        }
        sb.Indent().NewLine();


        switch (cacheCount)
        {
            case 0:
                // default instance
                sb.Append("internal static readonly CommandFactory").Append(index).Append(" Instance = new();").NewLine();
                break;
            case 1:
                // default instance, but we named it slightly differently because we were expecting more trouble
                sb.Append("internal static readonly CommandFactory").Append(index).Append(" Instance0 = new();").NewLine();
                break;
            default:
                // per-usage concrete sub-type
                sb.Append("// these represent different call-sites (and most likely all have different SQL etc)").NewLine();
                for (int i = 0; i < cacheCount; i++)
                {
                    sb.Append("internal static readonly CommandFactory").Append(index).Append(".Cached").Append(i)
                        .Append(" Instance").Append(i).Append(" = new();").NewLine();
                }
                sb.NewLine();
                break;
        }

        if (Inspection.IsCancellationToken(type))
        {
            sb.Append("public override global::System.Threading.CancellationToken GetCancellationToken(").Append(declaredType).Append(" args) => args;").NewLine();
        }
        var flags = WriteArgsFlags.None;
        if (string.IsNullOrWhiteSpace(map))
        {
            flags = WriteArgsFlags.CanPrepare;
        }
        else
        {
            sb.Append("public override void AddParameters(in global::Dapper.UnifiedCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine();
            WriteArgs(in ctx, type, sb, WriteArgsMode.Add, map, ref flags);
            sb.Outdent().NewLine();

            sb.Append("public override void UpdateParameters(in global::Dapper.UnifiedCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine();
            WriteArgs(in ctx, type, sb, WriteArgsMode.Update, map, ref flags);
            sb.Outdent().NewLine();

            if ((flags & (WriteArgsFlags.NeedsRowCount | WriteArgsFlags.NeedsPostProcess)) != 0)
            {
                sb.Append("public override bool RequirePostProcess => true;").NewLine().NewLine();
            }

            if ((flags & (WriteArgsFlags.NeedsPostProcess | WriteArgsFlags.NeedsRowCount)) != 0)
            {
                sb.Append("public override void PostProcess(in global::Dapper.UnifiedCommand cmd, ").Append(declaredType).Append(" args, int rowCount)").Indent().NewLine();
                if ((flags & WriteArgsFlags.NeedsPostProcess) != 0)
                {
                    WriteArgs(in ctx, type, sb, WriteArgsMode.PostProcess, map, ref flags);
                }
                if ((flags & WriteArgsFlags.NeedsRowCount) != 0)
                {
                    WriteArgs(in ctx, type, sb, WriteArgsMode.SetRowCount, map, ref flags);
                }
                if (baseFactory != DapperBaseCommandFactory)
                {
                    sb.Append("base.PostProcess(in cmd, args, rowCount);").NewLine();
                }
                sb.Outdent().NewLine();
            }

            if ((flags & WriteArgsFlags.HasCancellation) != 0)
            {
                sb.Append("public override global::System.Threading.CancellationToken GetCancellationToken(").Append(declaredType).Append(" args)")
                    .Indent().NewLine();
                WriteArgs(in ctx, type, sb, WriteArgsMode.GetCancellationToken, map, ref flags);
                sb.Outdent().NewLine();
            }
        }

        if ((flags & WriteArgsFlags.CanPrepare) != 0)
        {
            sb.Append("public override bool CanPrepare => true;").NewLine();
        }

        if (cacheCount != 0)
        {
            if ((flags & WriteArgsFlags.NeedsTest) != 0)
            {
                // I hope to never see this, but I'd rather know than not
                sb.Append("#error writing cache, but per-parameter test is needed; this isn't your fault - please report this! for now, mark the offending usage with [CacheCommand(false)]").NewLine();
            }

            // provide overrides to fetch/store cached commands
            WriteGetCommandHeader(sb, declaredType);
            if (additionalCommandState is not null && additionalCommandState.HasCommandProperties)
            {
                sb.Indent()
                    .NewLine().Append("var cmd = TryReuse(ref Storage, sql, commandType, args);")
                    .NewLine().Append("if (cmd is null)").Indent()
                    .NewLine().Append("cmd = base.GetCommand(connection, sql, commandType, args);");
                WriteCommandProperties(ctx, sb, "cmd", additionalCommandState.CommandProperties);
                sb.Outdent().NewLine().Append("return cmd;").Outdent();
            }
            else
            {
                sb.Indent(false).NewLine().Append(" => TryReuse(ref Storage, sql, commandType, args) ?? base.GetCommand(connection, sql, commandType, args);").Outdent(false);
            }
            sb.NewLine().NewLine().Append("public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);").NewLine();

            if (cacheCount == 1)
            {
                sb.Append("private static global::System.Data.Common.DbCommand? Storage;").NewLine();
            }
            else
            {
                sb.Append("protected abstract ref global::System.Data.Common.DbCommand? Storage {get;}").NewLine().NewLine();

                for (int i = 0; i < cacheCount; i++)
                {
                    sb.Append("internal sealed class Cached").Append(i).Append(" : CommandFactory").Append(index).Indent().NewLine()
                        .Append("protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;").NewLine()
                        .Append("private static global::System.Data.Common.DbCommand? s_Storage;").NewLine()
                        .Outdent().NewLine();
                }
            }
        }
        else if (additionalCommandState is not null && additionalCommandState.HasCommandProperties)
        {
            WriteGetCommandHeader(sb, declaredType).Indent().NewLine().Append("var cmd = base.GetCommand(connection, sql, commandType, args);");
            WriteCommandProperties(ctx, sb, "cmd", additionalCommandState.CommandProperties);
            sb.NewLine().Append("return cmd;").Outdent();
        }

        sb.Outdent().NewLine().NewLine();

        static CodeWriter WriteGetCommandHeader(CodeWriter sb, string declaredType) => sb.NewLine()
            .Append("public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,").Indent(false).NewLine()
            .Append("string sql, global::System.Data.CommandType commandType, ")
            .Append(declaredType).Append(" args)").Outdent(false);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
    private static void WriteCommandProperties(in GenerateState ctx, CodeWriter sb, string source, ImmutableArray<CommandProperty> properties, int index = 0)
    {
        foreach (var grp in properties.GroupBy(x => x.CommandType, SymbolEqualityComparer.Default))
        {
            var type = (INamedTypeSymbol)grp.Key!;
            bool isDbCmd = type is
            {
                Name: "DbCommand", ContainingType: null, Arity: 0, TypeKind: TypeKind.Class, ContainingNamespace:
                {
                    Name: "Common",
                    ContainingNamespace:
                    {
                        Name: "Data",
                        ContainingNamespace:
                        {
                            Name: "System",
                            ContainingNamespace.IsGlobalNamespace: true
                        }
                    }
                }
            };

            bool firstForType = true; // defer starting the if-test in case all invalid
            foreach (var prop in grp)
            {
                if (IsReserved(prop.Name))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.CommandPropertyReserved, prop.Location, prop.Name));
                    continue;
                }
                else if (!HasPublicSettableInstanceMember(type, prop.Name))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.CommandPropertyNotFound, prop.Location, type.Name, prop.Name));
                    continue;
                }
                if (firstForType && !isDbCmd)
                {
                    sb.NewLine().Append("if (cmd is ").Append(type).Append(" cmd").Append(index).Append(")").Indent();
                    firstForType = false;
                }

                sb.NewLine();
                if (isDbCmd) sb.Append(source);
                else sb.Append("cmd").Append(index);
                sb.Append(".").Append(prop.Name).Append(" = ");
                switch (prop.Value)
                {
                    case null:
                        sb.Append("null");
                        break;
                    case bool b:
                        sb.Append(b);
                        break;
                    case string s:
                        sb.AppendVerbatimLiteral(s);
                        break;
                    case int i:
                        sb.Append(i);
                        break;
                    default:
                        sb.Append(Convert.ToString(prop.Value, CultureInfo.InvariantCulture));
                        break;
                }
                sb.Append(";").NewLine();
            }
            if (!firstForType && !isDbCmd) // at least one was emitted; close the type test
            {
                sb.Outdent();
                index++;
            }
        }

        static bool HasPublicSettableInstanceMember(ITypeSymbol type, string name)
        {
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member.Name != name || member.DeclaredAccessibility != Accessibility.Public) continue;
                return member.Kind switch
                {
                    SymbolKind.Field when member is IFieldSymbol field => field.IsReadOnly,
                    SymbolKind.Property when member is IPropertySymbol prop => prop.SetMethod is not null,
                    _ => false,
                };
            }
            return false;
        }

        static bool IsReserved(string name)
        {
            switch (name)
            {
                // pretty much everything on DbCommand
                case nameof(DbCommand.CommandText):
                case nameof(DbCommand.CommandTimeout):
                case nameof(DbCommand.CommandType):
                case nameof(DbCommand.Connection):
                case nameof(DbCommand.Parameters):
                case nameof(DbCommand.Site):
                case nameof(DbCommand.Transaction):
                case nameof(DbCommand.UpdatedRowSource):
                // see SpecialCommandFlags
                case "InitialLONGFetchSize":
                case "BindByName":
                    return true;
                default:
                    return false;
            }
        }
    }

    private static void WriteRowFactory(in GenerateState context, CodeWriter sb, ITypeSymbol type, int index)
    {
        var map = MemberMap.CreateForResults(type);
        if (map is null) return;

        var members = map.Members;

        if (members.IsDefaultOrEmpty && map.Constructor is null && map.FactoryMethod is null)
        {
            // error is emitted, but we still generate default RowFactory to not emit more errors for this type
            WriteRowFactoryHeader();
            WriteRowFactoryFooter();

            return;
        }

        var hasInitOnlyMembers = members.Any(member => member.IsInitOnly);
        var hasRequiredMembers = members.Any(member => member.IsRequired);
        var hasGetOnlyMembers = members.Any(member => member is { IsGettable: true, IsSettable: false, IsInitOnly: false });
        var useConstructorDeferred = map.Constructor is not null;
        var useFactoryMethodDeferred = map.FactoryMethod is not null;
        
        // Implementation detail: 
        // constructor takes advantage over factory method.
        var useDeferredConstruction = useConstructorDeferred || useFactoryMethodDeferred || hasInitOnlyMembers || hasGetOnlyMembers || hasRequiredMembers;

        WriteRowFactoryHeader();

        WriteTokenizeMethod();
        WriteReadMethod();

        WriteRowFactoryFooter();

        void WriteRowFactoryHeader()
        {
            sb.Append("private sealed class RowFactory").Append(index).Append(" : global::Dapper.RowFactory").Append("<").Append(type).Append(">")
            .Indent().NewLine()
            .Append("internal static readonly RowFactory").Append(index).Append(" Instance = new();").NewLine()
            .Append("private RowFactory").Append(index).Append("() {}").NewLine();
        }
        void WriteRowFactoryFooter()
        {
            sb.Outdent().NewLine().NewLine();
        }

        void WriteTokenizeMethod()
        {
            sb.Append("public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)").Indent().NewLine();
            sb.Append("for (int i = 0; i < tokens.Length; i++)").Indent().NewLine()
                .Append("int token = -1;").NewLine()
                .Append("var name = reader.GetName(columnOffset);").NewLine()
                .Append("var type = reader.GetFieldType(columnOffset);").NewLine()
                .Append("switch (NormalizedHash(name))").Indent().NewLine();

            int token = 0;
            foreach (var member in members)
            {
                var dbName = member.DbName;
                sb.Append("case ").Append(StringHashing.NormalizedHash(dbName))
                    .Append(" when NormalizedEquals(name, ")
                    .AppendVerbatimLiteral(StringHashing.Normalize(dbName)).Append("):").Indent(false).NewLine()
                    .Append("token = type == typeof(").Append(Inspection.MakeNonNullable(member.CodeType)).Append(") ? ").Append(token)
                    .Append(" : ").Append(token + map.Members.Length).Append(";")
                    .Append(token == 0 ? " // two tokens for right-typed and type-flexible" : "").NewLine()
                    .Append("break;").Outdent(false).NewLine();
                token++;
            }
            sb.Outdent().NewLine()
                .Append("tokens[i] = token;").NewLine()
                .Append("columnOffset++;").NewLine();
            sb.Outdent().NewLine().Append("return null;").Outdent().NewLine();
        }
        void WriteReadMethod()
        {
            const string DeferredConstructionVariableName = "value";

            sb.Append("public override ").Append(type).Append(" Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)").Indent().NewLine();

            int token = 0;
            var deferredMethodArgumentsOrdered = new SortedList<int, string>();

            if (useDeferredConstruction)
            {
                // don't create an instance now, but define the variables to create an instance later like 
                // ```
                // Type? member0 = default;
                // Type? member1 = default;
                // ```

                foreach (var member in members)
                {
                    var variableName = DeferredConstructionVariableName + token;

                    if (Inspection.CouldBeNullable(member.CodeType)) sb.Append(CodeWriter.GetTypeName(member.CodeType.WithNullableAnnotation(NullableAnnotation.Annotated)));
                    else sb.Append(CodeWriter.GetTypeName(member.CodeType));

                    sb.Append(' ').Append(variableName).Append(" = default")
                        // if "default" will violate NRT: add a !
                        .Append(member.CodeType.IsReferenceType && member.CodeType.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "")
                        .Append(";").NewLine();
                    
                    if (useConstructorDeferred && member.ConstructorParameterOrder is not null)
                    {
                        deferredMethodArgumentsOrdered.Add(member.ConstructorParameterOrder.Value, variableName);
                    }
                    else if (useFactoryMethodDeferred && member.FactoryMethodParameterOrder is not null)
                    {
                        deferredMethodArgumentsOrdered.Add(member.FactoryMethodParameterOrder.Value, variableName);
                    }

                    token++;
                }
            }
            else
            {
                // we are not using a constructor, so we need to create an instance now
                sb.Append(type.NullableAnnotation == NullableAnnotation.Annotated
                    ? type.WithNullableAnnotation(NullableAnnotation.None) : type).Append(" result = new();").NewLine();
            }

            sb.Append("foreach (var token in tokens)").Indent().NewLine()
            .Append("switch (token)").Indent().NewLine();

            token = 0;
            foreach (var member in members)
            {
                var memberType = member.CodeType;

                member.GetDbType(out var readerMethod);
                var nullCheck = Inspection.CouldBeNullable(memberType) ? $"reader.IsDBNull(columnOffset) ? ({CodeWriter.GetTypeName(memberType.WithNullableAnnotation(NullableAnnotation.Annotated))})null : " : "";
                sb.Append("case ").Append(token).Append(":").NewLine().Indent(false);

                // write `result.X = ` or `member0 = `
                if (useDeferredConstruction) sb.Append(DeferredConstructionVariableName).Append(token);
                else sb.Append("result.").Append(member.CodeName);
                sb.Append(" = ");

                sb.Append(nullCheck);
                if (readerMethod is null)
                {
                    sb.Append("reader.GetFieldValue<").Append(memberType).Append(">(columnOffset);");
                }
                else
                {
                    sb.Append("reader.").Append(readerMethod).Append("(columnOffset);");
                }


                sb.NewLine().Append("break;").NewLine().Outdent(false)
                    .Append("case ").Append(token + map.Members.Length).Append(":").NewLine().Indent(false);

                // write `result.X = ` or `member0 = `
                if (useDeferredConstruction) sb.Append(DeferredConstructionVariableName).Append(token);
                else sb.Append("result.").Append(member.CodeName);

                sb.Append(" = ")
                    .Append(nullCheck)
                    .Append("GetValue<")
                    .Append(Inspection.MakeNonNullable(memberType)).Append(">(reader, columnOffset);").NewLine()
                    .Append("break;").NewLine().Outdent(false);

                token++;
            }

            sb.Outdent().NewLine().Append("columnOffset++;").NewLine().Outdent().NewLine();

            if (useDeferredConstruction)
            {
                // create instance using constructor or factory method. like
                // ```
                // return new Type(member0, member1, member2, ...)
                // {
                //     SettableMember1 = member3,
                //     SettableMember2 = member4,
                // }
                // ```
                // or in case of factory method:
                // return Type.Create(member0, member1, member2, ...)
                // ```
                
                if (useConstructorDeferred)
                {
                    // `return new Type(member0, member1, member2, ...);`
                    sb.Append("return new ").Append(type).Append('('); 
                    WriteDeferredMethodArgs();
                    sb.Append(')');
                    WriteDeferredInitialization();
                    sb.Append(";").Outdent();
                }
                else if (useFactoryMethodDeferred)
                {
                    // `return Type.FactoryCreate(member0, member1, member2, ...);`
                    sb.Append("return ").Append(type)
                      .Append('.').Append(map.FactoryMethod!.Name).Append('(');
                    WriteDeferredMethodArgs();
                    sb.Append(')').Append(";").Outdent();
                }
                else
                {
                    // left case is GetOnly or InitOnly - we can use only init syntax like:
                    // return new Type
                    // {
                    //      Member1 = value1,
                    //      Member2 = value2
                    // }
                    sb.Append("return new ").Append(type);
                    WriteDeferredInitialization();
                    sb.Append(";").Outdent();
                }

                void WriteDeferredInitialization()
                {   
                    // if all members are constructor arguments, no need to set them again
                    if (deferredMethodArgumentsOrdered!.Count == members.Length) return;
                    
                    sb.Indent().NewLine();
                    token = -1;
                    foreach (var member in members)
                    {
                        token++;
                        if (member.ConstructorParameterOrder is not null) continue; // already used in constructor arguments
                        sb.Append(member.CodeName).Append(" = ").Append(DeferredConstructionVariableName).Append(token).Append(',').NewLine();
                    }
                    sb.Outdent(withScope: false).Append("}");
                }
                
                void WriteDeferredMethodArgs()
                {
                    if (deferredMethodArgumentsOrdered!.Count == 0) return;
                    
                    // write `member0, member1, member2, ...` part of method
                    foreach (var constructorArg in deferredMethodArgumentsOrdered!)
                    {
                        sb.Append(constructorArg.Value).Append(", ");
                    }
                    sb.RemoveLast(2); // remove last ', ' generated in the loop
                }
            }
            else
            {
                // return instance constructed before
                sb.Append("return result;").NewLine().Outdent().NewLine();
            }
        }
    }

    [Flags]
    enum WriteArgsFlags
    {
        None = 0,
        NeedsTest = 1 << 0,
        NeedsPostProcess = 1 << 1,
        NeedsRowCount = 1 << 2,
        CanPrepare = 1 << 3,
        HasCancellation = 1 << 4,
    }

    enum WriteArgsMode
    {
        Add, Update, PostProcess,
        SetRowCount,
        GetCancellationToken
    }

    private static void WriteArgs(in GenerateState ctx, ITypeSymbol? parameterType, CodeWriter sb, WriteArgsMode mode, string map, ref WriteArgsFlags flags)
    {
        if (parameterType is null)
        {
            return;
        }

        var source = "args";

        if (parameterType.IsAnonymousType)
        {
            sb.Append("var typed = Cast(args, ");
            AppendShapeLambda(sb, parameterType);
            sb.Append("); // expected shape").NewLine();
            source = "typed";
        }

        if (mode == WriteArgsMode.Add)
        {   // we'll calculate this; assume we can, and claw backwards from there
            flags |= WriteArgsFlags.CanPrepare;
        }

        bool first = true, firstTest = true;
        int parameterIndex = 0;
        var memberMap = MemberMap.CreateForParameters(parameterType);
        if (memberMap is null or { Members.IsDefaultOrEmpty: true }) return;

        foreach (var member in memberMap.Members)
        {
            if (member.IsCancellation)
            {
                if (mode == WriteArgsMode.GetCancellationToken)
                {
                    sb.Append("return ").Append(source).Append(".").Append(member.CodeName).Append(";");
                }
                else
                {
                    flags |= WriteArgsFlags.HasCancellation;
                    continue;
                }
            }

            if (member.IsRowCount)
            {
                flags |= WriteArgsFlags.NeedsRowCount;
                if (mode == WriteArgsMode.SetRowCount)
                {
                    sb.Append(source).Append(".").Append(member.CodeName).Append(" = rowCount;").NewLine();
                }
            }
            if (mode == WriteArgsMode.SetRowCount || member.IsRowCount)
            {
                // row-count mode *only* does the above, and row-count members are *only*
                // used by that; they are not treated as routine parameters
                continue;
            }

            if (!SqlTools.IncludeParameter(map, member.CodeName, out var test))
            {
                continue; // not required
            }
            var direction = member.Direction;
            if (mode == WriteArgsMode.PostProcess)
            {
                switch (direction)
                {
                    case ParameterDirection.Output:
                    case ParameterDirection.InputOutput:
                    case ParameterDirection.ReturnValue:
                        break; // fine, we'll look at that
                    default:
                        parameterIndex++;
                        continue; // we don't need to know
                }
            }

            if (first && mode != WriteArgsMode.GetCancellationToken)
            {
                sb.Append("var ps = cmd.Parameters;").NewLine();
                switch (mode)
                {
                    case WriteArgsMode.Add:
                        sb.Append("global::System.Data.Common.DbParameter p;").NewLine();
                        break;
                }
                first = false;
            }
            else if (mode == WriteArgsMode.Add)
            {
                // space each param out a bit
                sb.NewLine();
            }

            if (test)
            {
                // add is seeing this for the first time
                if (firstTest)
                {
                    sb.Append("var sql = cmd.CommandText;").NewLine().Append("var commandType = cmd.CommandType;").NewLine();
                    flags |= WriteArgsFlags.NeedsTest;
                    firstTest = false;
                }
                sb.Append("if (Include(sql, commandType, ").AppendVerbatimLiteral(member.DbName).Append("))").Indent().NewLine();
            }
            switch (mode)
            {
                case WriteArgsMode.Add:
                    sb.Append("p = cmd.CreateParameter();").NewLine();
                    sb.Append("p.ParameterName = ").AppendVerbatimLiteral(member.DbName).Append(";").NewLine();

                    if (member.DapperSpecialType is DapperSpecialType.DbString)
                    {
                        ctx.GeneratorContext.IncludeGenerationType(IncludedGeneration.DbStringHelpers);

                        sb.Append("global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(p, ")
                          .Append(source).Append(".").Append(member.DbName).Append(");").NewLine();

                        sb.Append("ps.Add(p);").NewLine(); // dont forget to add parameter to command parameters collection
                        break;
                    }

                    var dbType = member.GetDbType(out _);
                    var size = member.TryGetValue<int>("Size");
                    bool useSetValueWithDefaultSize = false;
                    if (dbType is not null)
                    {
                        sb.Append("p.DbType = global::System.Data.DbType.").Append(dbType.GetValueOrDefault().ToString()).Append(";").NewLine();
                        if (size is null)
                        {
                            switch (dbType.GetValueOrDefault())
                            {
                                case DbType.Binary:
                                case DbType.String:
                                case DbType.AnsiString:
                                    if (member.CodeType.SpecialType == SpecialType.System_String)
                                    {
                                        useSetValueWithDefaultSize = true;
                                    }
                                    else
                                    {
                                        size = -1; // default to [n]varchar(max)/varbinary(max)
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // prepare requires all args to have a type (it also requires all
                        // string/binary args to have a size, but: we've set that)
                        flags &= ~WriteArgsFlags.CanPrepare;
                    }
                    AppendDbParameterSetting(sb, "Size", size);
                    AppendDbParameterSetting(sb, "Precision", member.TryGetValue<byte>("Precision"));
                    AppendDbParameterSetting(sb, "Scale", member.TryGetValue<byte>("Scale"));

                    sb.Append("p.Direction = global::System.Data.ParameterDirection.").Append(direction switch
                    {
                        ParameterDirection.Input => nameof(ParameterDirection.Input),
                        ParameterDirection.InputOutput => nameof(ParameterDirection.InputOutput),
                        ParameterDirection.Output => nameof(ParameterDirection.Output),
                        ParameterDirection.ReturnValue => nameof(ParameterDirection.ReturnValue),
                        _ => direction.ToString(),
                    }).Append(";").NewLine();
                    // the actual value expression
                    switch (direction)
                    {
                        case ParameterDirection.Input:
                        case ParameterDirection.InputOutput:
                            if (useSetValueWithDefaultSize)
                            {
                                sb.Append("SetValueWithDefaultSize(p, ").Append(source).Append(".").Append(member.CodeName).Append(");").NewLine();
                            }
                            else
                            {
                                sb.Append("p.Value = ").Append("AsValue(").Append(source).Append(".").Append(member.CodeName).Append(");").NewLine();
                            }
                            break;
                        default:
                            sb.Append("p.Value = global::System.DBNull.Value;").NewLine();
                            break;
                    }
                    sb.Append("ps.Add(p);").NewLine();

                    switch (direction)
                    {
                        case ParameterDirection.InputOutput:
                        case ParameterDirection.Output:
                        case ParameterDirection.ReturnValue:
                            flags |= WriteArgsFlags.NeedsPostProcess;
                            break;
                    }
                    break;
                case WriteArgsMode.Update:
                    if (member.DapperSpecialType is DapperSpecialType.DbString)
                    {
                        ctx.GeneratorContext.IncludeGenerationType(IncludedGeneration.DbStringHelpers);

                        sb.Append("global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter")
                            .Append("(ps[").Append(parameterIndex).Append("], ")
                            .Append(source).Append(".").Append(member.CodeName)
                            .Append(");").NewLine();

                        break;
                    }

                    sb.Append("ps[");
                    if ((flags & WriteArgsFlags.NeedsTest) != 0) sb.AppendVerbatimLiteral(member.DbName);
                    else sb.Append(parameterIndex);
                    sb.Append("].Value = ");
                    switch (direction)
                    {
                        case ParameterDirection.Input:
                        case ParameterDirection.InputOutput:
                            sb.Append("AsValue(").Append(source).Append(".").Append(member.CodeName).Append(");").NewLine();
                            break;
                        default:
                            sb.Append("global::System.DBNull.Value;").NewLine();
                            break;

                    }
                    break;
                case WriteArgsMode.PostProcess:
                    // we already eliminated args that we don't need to look at
                    sb.Append(source).Append(".").Append(member.CodeName).Append(" = Parse<")
                        .Append(member.CodeType).Append(">(ps[");
                    if ((flags & WriteArgsFlags.NeedsTest) != 0) sb.AppendVerbatimLiteral(member.DbName);
                    else sb.Append(parameterIndex);
                    sb.Append("].Value);").NewLine();

                    break;
            }
            if (test)
            {
                sb.Outdent().NewLine();
            }
            parameterIndex++;
        }
    }

    static void AppendDbParameterSetting(CodeWriter sb, string memberName, int? value)
    {
        if (value is not null)
        {
            sb.Append("p.").Append(memberName).Append(" = ").Append(value.GetValueOrDefault()).Append(";").NewLine();
        }
    }
    static void AppendDbParameterSetting(CodeWriter sb, string memberName, byte? value)
    {
        if (value is not null)
        {
            sb.Append("p.").Append(memberName).Append(" = ").Append(value.GetValueOrDefault()).Append(";").NewLine();
        }
    }

    private static void AppendShapeLambda(CodeWriter sb, ITypeSymbol parameterType)
    {
        var members = parameterType.GetMembers();
        int count = CodeWriter.CountGettableInstanceMembers(members);
        switch (count)
        {
            case 0:
                sb.Append("static () => (object?)null");
                break;
            default:
                bool first = true;
                sb.Append("static () => new {");
                foreach (var member in members)
                {
                    if (CodeWriter.IsGettableInstanceMember(member, out var type))
                    {
                        sb.Append(first ? " " : ", ").Append(member.Name).Append(" = default(").Append(type).Append(")");
                        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None)
                        {
                            sb.Append("!");
                        }
                        first = false;
                    }
                }
                sb.Append(" }");
                break;
        }
    }

    private static SpecialCommandFlags GetSpecialCommandFlags(ITypeSymbol type)
    {
        // check whether these command-types need special handling
        var flags = SpecialCommandFlags.None;
        foreach (var member in type.GetMembers())
        {
            switch (member.Name)
            {
                // just do a quick check for now, will be close enough
                case "BindByName" when IsSettableInstanceProperty(member, SpecialType.System_Boolean):
                    flags |= SpecialCommandFlags.BindByName;
                    break;
                case "InitialLONGFetchSize" when IsSettableInstanceProperty(member, SpecialType.System_Int32):
                    flags |= SpecialCommandFlags.InitialLONGFetchSize;
                    break;
            }
        }
        return flags;

        static bool IsSettableInstanceProperty(ISymbol? symbol, SpecialType type) =>
            symbol is IPropertySymbol prop && prop.DeclaredAccessibility == Accessibility.Public
            && prop.SetMethod is { DeclaredAccessibility: Accessibility.Public }
            && prop.Type.SpecialType == type
            && !prop.IsIndexer && !prop.IsStatic;
    }

    [Flags]
    private enum SpecialCommandFlags
    {
        None = 0,
        BindByName = 1 << 0,
        InitialLONGFetchSize = 1 << 1,
    }

    private static ImmutableArray<ITypeSymbol> IdentifyDbCommandTypes(Compilation compilation, out bool needsPrepare)
    {
        needsPrepare = false;
        var dbCommand = compilation.GetTypeByMetadataName("System.Data.Common.DbCommand");
        if (dbCommand is null)
        {
            // if we can't find DbCommand, we're out of luck
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        var pending = new Queue<INamespaceOrTypeSymbol>();
        foreach (var assemblyName in compilation.References)
        {
            if (assemblyName is null) continue;
            var ns = compilation.GetAssemblyOrModuleSymbol(assemblyName) switch
            {
                IAssemblySymbol assembly => assembly.GlobalNamespace,
                IModuleSymbol module => module.GlobalNamespace,
                _ => null
            };
            if (ns is not null)
            {
                pending.Enqueue(ns);
            }
        }
        var found = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        while (pending.Count != 0)
        {
            var current = pending.Dequeue();
            foreach (var member in current.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        pending.Enqueue(ns);
                        break;
                    case ITypeSymbol type:
                        // only interested in public non-static classes
                        if (!type.IsStatic && type.TypeKind == TypeKind.Class && type.DeclaredAccessibility == Accessibility.Public)
                        {
                            // note we're not checking for nested types; that seems incredibly unlikely for ADO.NET types
                            if (IsDerived(type, dbCommand))
                            {
                                found.Add(type);
                            }
                        }
                        break;
                }
            }
        }

        foreach (var type in found)
        {
            if (GetSpecialCommandFlags(type) != SpecialCommandFlags.None)
            {
                needsPrepare = true;
                break; // only need at least one
            }
        }
        return found.ToImmutableArray();

        static bool IsDerived(ITypeSymbol? type, ITypeSymbol baseType)
        {
            while (type is not null && type.SpecialType != SpecialType.System_Object)
            {
                type = type.BaseType;
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal abstract class SourceState
    {
        public Location? Location { get; }
        protected SourceState(Location? location) => Location = location;
    }

    internal sealed class FaultSourceState : SourceState
    {
        public Exception Fault { get; }

        public FaultSourceState(Location? location, Exception fault) : base(location)
            => Fault = fault;
    }

    internal sealed class SuccessSourceState : SourceState
    {
        public new Location Location => base.Location!; // assert non-null

        public OperationFlags Flags { get; }
        public string? Sql { get; }
        public string ParameterMap { get; }
        public IMethodSymbol Method { get; }
        public ITypeSymbol? ResultType { get; }
        public ITypeSymbol? ParameterType { get; }
        public AdditionalCommandState? AdditionalCommandState { get; }

        public SuccessSourceState(Location location, IMethodSymbol method, OperationFlags flags, string? sql,
            ITypeSymbol? resultType, ITypeSymbol? parameterType, string parameterMap,
            AdditionalCommandState? additionalCommandState) : base(location)
        {
            Flags = flags;
            Sql = sql;
            ResultType = resultType;
            ParameterType = parameterType;
            Method = method;
            ParameterMap = parameterMap;
            AdditionalCommandState = additionalCommandState;
        }

        public (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation, AdditionalCommandState? AdditionalCommandState) Group()
            => new(Flags, Method, ParameterType, ParameterMap, (Flags & (OperationFlags.CacheCommand | OperationFlags.IncludeLocation)) == 0 ? null : Location, AdditionalCommandState);
    }
    private sealed class CommonComparer : LocationComparer, IEqualityComparer<(OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation, AdditionalCommandState? AdditionalCommandState)>
    {
        public static readonly CommonComparer Instance = new();
        private CommonComparer() { }

        public bool Equals(

            (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation, AdditionalCommandState? AdditionalCommandState) x,
            (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation, AdditionalCommandState? AdditionalCommandState) y) => x.Flags == y.Flags
                && x.ParameterMap == y.ParameterMap
                && SymbolEqualityComparer.Default.Equals(x.Method, y.Method)
                && SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType)
                && x.UniqueLocation == y.UniqueLocation
                && Equals(x.AdditionalCommandState, y.AdditionalCommandState);

        public int GetHashCode((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation, AdditionalCommandState? AdditionalCommandState) obj)
        {
            var hash = (int)obj.Flags;
            hash *= -47;
            hash += obj.ParameterMap.GetHashCode();
            hash *= -47;
            hash += SymbolEqualityComparer.Default.GetHashCode(obj.Method);
            hash *= -47;
            if (obj.ParameterType is not null)
            {
                hash += SymbolEqualityComparer.Default.GetHashCode(obj.ParameterType);
            }
            hash *= -47;
            if (obj.UniqueLocation is not null)
            {
                hash += obj.UniqueLocation.GetHashCode();
            }
            hash *= -47;
            if (obj.AdditionalCommandState is not null)
            {
                hash += obj.AdditionalCommandState.GetHashCode();
            }
            return hash;
        }
    }
}
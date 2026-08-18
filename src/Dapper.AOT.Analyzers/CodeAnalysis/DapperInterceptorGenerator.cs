using Dapper.CodeAnalysis.Abstractions;
using Dapper.CodeAnalysis.Model;
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
using System.Buffers;
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
        // note the cached values are all plain data (see ModelShapeTests): symbols are fully
        // projected during parse, and the raw Compilation must not feed the output step
        var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                    .Where(x => x is not null)
                    .Select((x, _) => x!);
        var env = context.CompilationProvider.Select(static (c, _) => CreateEnvironment(c));
        var combined = env.Combine(nodes.Collect());
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
    // see https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md#file-paths
    // (the parse-time projection of GenerateState.GetInterceptorFilePath)
    private static string InterceptorFilePath(in ParseState ctx, Location location)
    {
        if (location.SourceTree is not { } tree) return "";
        return ctx.SemanticModel.Compilation.Options.SourceReferenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
    }

    // expandable (in @ids) and custom (ICustomQueryParameter) members bind themselves,
    // contributing an unknowable number of parameters; the guards below treat them alike
    private static bool HasSelfBindingMember(ParamPlan? plan)
    {
        if (plan is not null)
        {
            foreach (var member in plan.Members)
            {
                if (member.IsMapped && (member.IsExpandable || member.IsCustom)) return true;
            }
        }
        return false;
    }

    private static bool HasNonInputMember(ParamPlan? plan)
    {
        if (plan is not null)
        {
            foreach (var member in plan.Members)
            {
                if (member.IsMapped && !member.IsCancellation && !member.IsRowCount
                    && member.Direction != System.Data.ParameterDirection.Input) return true;
            }
        }
        return false;
    }

    private static InterceptedMethod ProjectMethod(IMethodSymbol method)
    {
        var args = method.Parameters;
        var parameters = new MethodParam[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            parameters[i] = new MethodParam(CodeWriter.GetAppendTypeName(args[i].Type), args[i].Name);
        }
        // the NRT shim over Dapper oddities: is the (awaited) return value annotated?
        bool needsNullForgiving = method.ReturnType.IsAsync(out var awaited)
            ? awaited is not null && awaited.NullableAnnotation != NullableAnnotation.Annotated
            : method.ReturnType.NullableAnnotation != NullableAnnotation.Annotated;
        return new InterceptedMethod(CodeWriter.GetAppendTypeName(method.ReturnType), method.Name,
            method.IsExtensionMethod, method.Arity, needsNullForgiving, new EquatableArray<MethodParam>(parameters));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Chosen API")]
    internal SourceState? Parse(ParseState ctx)
    {
        try
        {
            if (ctx.Node is not InvocationExpressionSyntax ie
                || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op
                || !op.IsDapperMethod(out var flags)
                || flags.HasAny(OperationFlags.DoNotGenerate)
                || !Inspection.IsEnabled(ctx, op, Types.DapperAotAttribute, out var aotAttribExists))
            {
                return null;
            }
            if (flags.HasAny(OperationFlags.NotAotSupported))
            {
                // not our API (yet); count it, so the scorecard stays honest
                return new SkippedSourceState(new LocationSnapshot(ie.GetLocation()), flags);
            }

            var location = DapperAnalyzer.SharedParseArgsAndFlags(ctx, op, ref flags, out var sql, out var argExpression, reportDiagnostic: null, out var resultType, exitFirstFailure: true);
            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                // diagnostics (from the analyzer's identical pass) told us to leave it alone
                return new SkippedSourceState(new LocationSnapshot(location), flags);
            }



            // additional result-type checks

            // perform SQL inspection
            var map = MemberMap.CreateForParameters(argExpression);
            var parameterMap = BuildParameterMap(ctx, op, sql, ref flags, map, location, out var parseFlags);

            var parameterPlan = ParamPlan.Create(argExpression?.Type);
            if (parameterPlan is { IsCollection: true, Element: { } element } && HasSelfBindingMember(element))
            {
                // multi-exec batch reuse updates parameters in-place, which cannot re-bind a
                // self-binding member; leave such call-sites on vanilla Dapper
                return new SkippedSourceState(new LocationSnapshot(location), flags);
            }
            if (HasSelfBindingMember(parameterPlan) && HasNonInputMember(parameterPlan))
            {
                // PostProcess addresses output/return parameters by *index*, and an expanded
                // list contributes a runtime-variable number of parameters before them; leave
                // such call-sites on vanilla Dapper rather than read back the wrong slot
                return new SkippedSourceState(new LocationSnapshot(location), flags);
            }
            if (flags.HasAny(OperationFlags.CacheCommand))
            {
                bool canBeCached = true;
                // need fixed text, command-type and parameters to be reusable
                if (string.IsNullOrWhiteSpace(sql) || parameterMap == "?" || !flags.HasAny(OperationFlags.StoredProcedure | OperationFlags.TableDirect | OperationFlags.Text))
                {
                    canBeCached = false;
                }
                else if (HasSelfBindingMember(parameterPlan))
                {
                    canBeCached = false; // self-binding members change the parameter shape per call
                }

                if (!canBeCached) flags &= ~OperationFlags.CacheCommand;
            }

            var additionalState = AdditionalCommandState.Parse(Inspection.GetSymbol(ctx, op), map, null);

            Debug.Assert(!flags.HasAny(OperationFlags.DoNotGenerate), "should have already exited");
            int languageVersion = ctx.Node.SyntaxTree.Options is CSharpParseOptions csOptions ? (int)csOptions.LanguageVersion : -1;
            return new SuccessSourceState(new LocationSnapshot(location), InterceptorFilePath(ctx, location), languageVersion,
                ProjectMethod(op.TargetMethod), flags, sql,
                RowPlan.Create(resultType, additionalState?.QueryColumns ?? default),
                parameterPlan, parameterMap, additionalState);
        }
        catch (Exception ex)
        {
            LocationSnapshot loc = default;
            try
            {
                loc = new LocationSnapshot(ctx.Node.GetLocation());
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


    internal static InterceptorEnvironment CreateEnvironment(Compilation compilation)
    {
        var dbCommandTypes = IdentifyDbCommandTypes(compilation, out var needsCommandPrep);
        EquatableArray<SpecialDbCommandType> special = default;
        if (!dbCommandTypes.IsDefaultOrEmpty)
        {
            var builder = new List<SpecialDbCommandType>();
            foreach (var type in dbCommandTypes)
            {
                var flags = GetSpecialCommandFlags(type);
                if (flags != SpecialCommandFlags.None)
                {
                    builder.Add(new SpecialDbCommandType(CodeWriter.GetAppendTypeName(type), type.Name,
                        (flags & SpecialCommandFlags.BindByName) != 0,
                        (flags & SpecialCommandFlags.InitialLONGFetchSize) != 0));
                }
            }
            if (builder.Count != 0) special = new(builder.ToArray());
        }
        var baseFactory = GetCommandFactory(compilation, out var canConstruct);
        return new InterceptorEnvironment(
            allowUnsafe: compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe,
            assemblyName: compilation.AssemblyName,
            hasInterceptsLocationAttribute: PreGeneratedCodeWriter.HasInterceptsLocationAttribute(compilation),
            needsCommandPrep: needsCommandPrep,
            baseCommandFactoryName: baseFactory,
            baseFactoryCanConstruct: canConstruct,
            specialCommandTypes: special,
            systemObjectPlan: ParamPlan.Create(compilation.GetSpecialType(SpecialType.System_Object))!);
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
        public const string InterceptorsNamespaces = nameof(InterceptorsNamespaces),
            InterceptorsPreviewNamespaces = nameof(InterceptorsPreviewNamespaces),
            CodegenNamespace = "Dapper.AOT";
        public static KeyValuePair<string, string> InterceptorsPreviewNamespacePair => new(InterceptorsPreviewNamespaces, CodegenNamespace);
        public static KeyValuePair<string, string> InterceptorsNamespacePair => new(InterceptorsNamespaces, CodegenNamespace);
    }

    private static bool CheckPrerequisites(in GenerateState ctx)
    {
        if (ctx.Nodes.IsDefaultOrEmpty) return false; // nothing to do

        // find the first enabled thing with a C# parse options
        var firstSuccess = ctx.Nodes.OfType<SuccessSourceState>().FirstOrDefault();
        if (firstSuccess is null || firstSuccess.LanguageVersion < 0) return false; // not C#

        bool success = true;

        var version = (LanguageVersion)firstSuccess.LanguageVersion;
        if (version != LanguageVersion.Default && version < LanguageVersion.CSharp11)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.LanguageVersionTooLow, null));
            success = false;
        }
        return success;
        
    }

    private void Generate(SourceProductionContext ctx, (InterceptorEnvironment Environment, ImmutableArray<SourceState> Nodes) state)
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
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownError, fault.Location.AsLocation(), ex.Message, ex.StackTrace));
        }

        int unsupported = 0, skippedViaDiagnostics = 0;
        foreach (var skip in ctx.Nodes.OfType<SkippedSourceState>())
        {
            if (skip.Flags.HasAny(OperationFlags.NotAotSupported)) unsupported++;
            else skippedViaDiagnostics++;
        }

        if (!CheckPrerequisites(ctx)) // also reports per-item diagnostics
        {
            // failed checks; nothing to generate, but still say what we saw - and every
            // enabled call-site (including ones we *could* have handled) goes unhandled
            if (!ctx.Nodes.IsDefaultOrEmpty)
            {
                int total = unsupported + skippedViaDiagnostics + ctx.Nodes.OfType<SuccessSourceState>().Count();
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InterceptorsGenerated, null, 0, total, unsupported, skippedViaDiagnostics, 0, 0, 0));
            }
            return;
        }

        var env = ctx.Environment;
        bool needsCommandPrep = env.NeedsCommandPrep;

        bool allowUnsafe = env.AllowUnsafe;
        var sb = new CodeWriter().Append("#nullable enable").NewLine()
            .Append("#pragma warning disable IDE0078 // unnecessary suppression is necessary").NewLine()
            .Append("#pragma warning disable CS9270 // SDK-dependent change to interceptors usage").NewLine()
            .Append("namespace ").Append(FeatureKeys.CodegenNamespace).Append(" // interceptors must be in a known namespace").Indent().NewLine()
            .Append("file static class DapperGeneratedInterceptors").Indent().NewLine();
        int methodIndex = 0, callSiteCount = 0;

        var factories = new CommandFactoryState(env.SystemObjectPlan);
        var readers = new RowReaderState();

        foreach (var grp in ctx.Nodes.OfType<SuccessSourceState>().Where(x => !x.Flags.HasAny(OperationFlags.DoNotGenerate)).GroupBy(x => x.Group(), CommonComparer.Instance))
        {
            // first, try to resolve the helper method that we're going to use for this
            var (flags, method, parameterPlan, parameterMap, _, additionalCommandState) = grp.Key;
            const bool useUnsafe = false;
            int usageCount = 0;

            foreach (var op in grp.OrderBy(row => row.Location, CommonComparer.Instance))
            {
                var loc = op.Location;
                sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                    .AppendVerbatimLiteral(op.InterceptorFilePath).Append(", ").Append(loc.StartLine + 1).Append(", ").Append(loc.StartChar + 1).Append(")]").NewLine();
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
                else if (method.IsExtension) sb.Append("this ");
                sb.Append(parameters[i].Type).Append(" ").Append(parameters[i].Name);
            }
            sb.Append(")").Indent().NewLine();
            sb.Append("// ").Append(flags.ToString()).NewLine();
            if (flags.HasAny(OperationFlags.HasParameters))
            {
                sb.Append("// takes parameter: ").Append(parameterPlan!.TypeName).NewLine();
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
            RowPlan? resultPlan = null;
            if (flags.HasAny(OperationFlags.TypedResult))
            {
                resultPlan = grp.First().ResultPlan!;
                sb.Append("// returns data: ").Append(resultPlan.TypeName).NewLine();
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
                    fixedSql = $"-- {origin.Location.MappedPath}#{origin.Location.MappedStartLine + 1}\r\n{fixedSql}";
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
                WriteGetRowParser(sb, resultPlan, readers, grp.Key.Flags);
            }
            else if (!TryWriteMultiExecImplementation(sb, flags, commandTypeMode, parameterPlan, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories, fixedSql, additionalCommandState))
            {
                WriteSingleImplementation(sb, method, resultPlan, flags, commandTypeMode, parameterPlan, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories, readers, fixedSql, additionalCommandState);
            }

            sb.Outdent().NewLine().NewLine();
        }

        var baseCommandFactory = env.BaseCommandFactoryName ?? DapperBaseCommandFactory;
        var canConstruct = env.BaseFactoryCanConstruct;
        if (needsCommandPrep || !canConstruct)
        {
            // at least one command-type needs special handling; do that
            sb.Append("private class CommonCommandFactory<T> : ").Append(baseCommandFactory).Append("<T>").Indent().NewLine();
            if (needsCommandPrep)
            {
                sb.Append("public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)").Indent().NewLine()
                .Append("var cmd = base.GetCommand(connection, sql, commandType, args);");
                int cmdTypeIndex = 0;
                foreach (var special in env.SpecialCommandTypes)
                {
                    sb.NewLine().Append("// apply special per-provider command initialization logic for ").Append(special.ShortName).NewLine()
                        .Append(cmdTypeIndex == 0 ? "" : "else ").Append("if (cmd is ").Append(special.TypeName).Append(" cmd").Append(cmdTypeIndex).Append(")").Indent().NewLine();
                    if (special.BindByName)
                    {
                        sb.Append("cmd").Append(cmdTypeIndex).Append(".BindByName = true;").NewLine();
                    }
                    if (special.InitialLONGFetchSize)
                    {
                        sb.Append("cmd").Append(cmdTypeIndex).Append(".InitialLONGFetchSize = -1;").NewLine();
                    }
                    sb.Outdent().NewLine();
                    cmdTypeIndex++;
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

        foreach (var tuple in readers)
        {
            WriteRowFactory(sb, tuple.Plan, tuple.Index, tuple.Flags);
        }

        foreach (var tuple in factories)
        {
            WriteCommandFactory(ctx, baseCommandFactory, sb, tuple.Plan, tuple.Index, tuple.Map, tuple.CacheCount, tuple.AdditionalCommandState);
        }

        sb.Outdent().Outdent(); // ends our generated file-scoped class and the namespace
        
        var preGeneratedCodeWriter = new PreGeneratedCodeWriter(sb, env.HasInterceptsLocationAttribute);
        preGeneratedCodeWriter.Write(ctx.GeneratorContext.IncludedGenerationTypes);

        ctx.AddSource((env.AssemblyName ?? "package") + ".generated.cs", sb.ToString());
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InterceptorsGenerated, null,
            callSiteCount, callSiteCount + unsupported + skippedViaDiagnostics, unsupported, skippedViaDiagnostics,
            methodIndex, factories.Count(), readers.Count()));
    }

    private static void WriteGetRowParser(CodeWriter sb, RowPlan? resultPlan, in RowReaderState readers, OperationFlags flags)
    {
        sb.Append("return ").AppendReader(resultPlan, readers, flags)
            .Append(".GetRowParser(reader, startIndex, length, returnNullIfFirstMissing);").NewLine();
    }

    private static void WriteCommandFactory(in GenerateState ctx, string baseFactory, CodeWriter sb, ParamPlan type, int index, string map, int cacheCount, AdditionalCommandState? additionalCommandState)
    {
        var declaredType = type.DeclaredType;
        sb.Append("private ").Append(cacheCount <= 1 ? "sealed" : "abstract").Append(" class CommandFactory").Append(index).Append(" : ")
            .Append(baseFactory).Append("<").Append(declaredType).Append(">");
        if (type.IsAnonymous)
        {
            sb.Append(" // ").Append(type.TypeName); // give the reader a clue
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

        if (type.IsCancellationTokenType)
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
                    .NewLine().Append("var cmd = TryReuseThreadStatic(ref Storage, sql, commandType, args, _cmdPool);")
                    .NewLine().Append("if (cmd is null)").Indent()
                    .NewLine().Append("cmd = base.GetCommand(connection, sql, commandType, args);");
                WriteCommandProperties(ctx, sb, "cmd", additionalCommandState.CommandProperties);
                sb.Outdent().NewLine().Append("return cmd;").Outdent();
            }
            else
            {
                sb.Indent(false).NewLine().Append(" => TryReuseThreadStatic(ref Storage, sql, commandType, args, _cmdPool) ?? base.GetCommand(connection, sql, commandType, args);").Outdent(false);
            }
            sb.NewLine().NewLine().Append("public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycleThreadStatic(ref Storage, command, _cmdPool);").NewLine();

            if (cacheCount == 1)
            {
                sb.Append("private static readonly DbCommandCache _cmdPool = new();").NewLine();
                sb.Append("[global::System.ThreadStatic] // note this works correctly with ref").NewLine();
                sb.Append("private static global::System.Data.Common.DbCommand? Storage;").NewLine();
            }
            else
            {
                sb.Append("private readonly DbCommandCache _cmdPool = new(); // note: per cache instance").NewLine();
                sb.Append("protected abstract ref global::System.Data.Common.DbCommand? Storage {get;}").NewLine().NewLine();

                for (int i = 0; i < cacheCount; i++)
                {
                    sb.Append("internal sealed class Cached").Append(i).Append(" : CommandFactory").Append(index).Indent().NewLine()
                        .Append("protected override ref global::System.Data.Common.DbCommand? Storage => ref s_Storage;").NewLine()
                        .Append("[global::System.ThreadStatic] // note this works correctly with ref-return").NewLine()
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
    private static void WriteCommandProperties(in GenerateState ctx, CodeWriter sb, string source, in EquatableArray<CommandProperty> properties, int index = 0)
    {
        foreach (var grp in properties.GroupBy(x => x.CommandTypeName, StringComparer.Ordinal))
        {
            bool isDbCmd = false, firstForType = true; // defer starting the if-test in case all invalid
            foreach (var prop in grp)
            {
                isDbCmd = prop.IsDbCommand;
                if (IsReserved(prop.Name))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.CommandPropertyReserved, prop.Location.AsLocation(), prop.Name));
                    continue;
                }
                else if (!prop.MemberExists)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.CommandPropertyNotFound, prop.Location.AsLocation(), prop.CommandTypeShortName, prop.Name));
                    continue;
                }
                if (firstForType && !isDbCmd)
                {
                    sb.NewLine().Append("if (cmd is ").Append(grp.Key).Append(" cmd").Append(index).Append(")").Indent();
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

    private static void WriteRowFactory(CodeWriter sb, RowPlan plan, int index, OperationFlags flags)
    {
        var members = plan.Members;
        var queryColumns = plan.QueryColumns;

        if (members.IsEmpty && !plan.UseConstructor && !plan.UseFactoryMethod)
        {
            // error is emitted, but we still generate default RowFactory to not emit more errors for this type
            WriteRowFactoryHeader();
            WriteRowFactoryFooter();

            return;
        }

        var useDeferredConstruction = plan.UseDeferredConstruction;

        WriteRowFactoryHeader();

        WriteTokenizeMethod();
        WriteReadMethod();

        WriteRowFactoryFooter();

        void WriteRowFactoryHeader()
        {
            sb.Append("private sealed class RowFactory").Append(index).Append(" : global::Dapper.RowFactory").Append("<").Append(plan.TypeName).Append(">")
            .Indent().NewLine();
            if (flags != 0)
            {
                sb.Append("// flags: ").Append(flags.ToString()).NewLine();
            }
            if (!queryColumns.IsDefault)
            {
                sb.Append("// query columns: ");
                for (int i = 0; i < queryColumns.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    var name = queryColumns[i];
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        sb.Append("(n/a)");
                    }
                    else if (CompiledRegex.SimpleName.IsMatch(name))
                    {
                        sb.Append(name);
                    }
                    else
                    {
                        sb.Append("'").Append(name).Append("'");
                    }
                }
                sb.NewLine();
            }
            sb.Append("internal static readonly RowFactory").Append(index).Append(" Instance = new();").NewLine()
            .Append("private RowFactory").Append(index).Append("() {}").NewLine();
        }
        void WriteRowFactoryFooter()
        {
            sb.Outdent().NewLine().NewLine();
        }

        void WriteTokenizeMethod()
        {
            sb.Append("public override object? Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)").Indent().NewLine();
            if (queryColumns.IsDefault) // need to apply full map
            {
                sb.Append("for (int i = 0; i < tokens.Length; i++)").Indent().NewLine()
                    .Append("int token = -1;").NewLine()
                    .Append("var name = reader.GetName(columnOffset);").NewLine()
                    .Append("var type = reader.GetFieldType(columnOffset);").NewLine()
                    .Append("switch (NormalizedHash(name))").Indent().NewLine();

                int token = 0;
                foreach (var member in members)
                {
                    if (member.IsMapped)
                    {
                        var dbName = member.DbName;
                        sb.Append("case ").Append(StringHashing.NormalizedHash(dbName))
                            .Append(" when NormalizedEquals(name, ")
                            .AppendVerbatimLiteral(StringHashing.Normalize(dbName)).Append("):").Indent(false).NewLine();
                        if (flags.HasAny(OperationFlags.StrictTypes))
                        {
                            sb.Append("token = ").Append(token).Append(";").Append(token == 0 ? " // note: strict types" : "");
                        }
                        else
                        {
                            sb.Append("token = type == typeof(").Append(member.TypeOfName).Append(") ? ").Append(token)
                            .Append(" : ").Append(token + plan.TotalMemberCount).Append(";")
                            .Append(token == 0 ? " // two tokens for right-typed and type-flexible" : "");
                        }
                        sb.NewLine().Append("break;").Outdent(false).NewLine();
                    }
                    token++;
                }
                sb.Outdent().NewLine()
                    .Append("tokens[i] = token;").NewLine()
                    .Append("columnOffset++;").NewLine()
                    .Outdent().NewLine();
            }
            else
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(tokens.Length >= ").Append(queryColumns.Length).Append(""", "Query columns count mismatch");""").NewLine();
                if (flags.HasAny(OperationFlags.StrictTypes))
                {
                    sb.Append("// (no mapping applied for strict types and pre-defined columns)").NewLine();
                }
                else
                {
                    sb.Append("// pre-defined columns, but still needs type map").NewLine();
                    sb.Append("for (int i = 0; i < tokens.Length; i++)").Indent().NewLine()
                        .Append("var type = reader.GetFieldType(columnOffset);").NewLine()
                        .Append("tokens[i] = i switch").Indent().NewLine();
                    for (int i = 0; i < members.Length;i++)
                    {
                        var member = members[i];
                        if (member.IsMapped)
                        {
                            sb.Append(i).Append(" => type == typeof(").Append(member.TypeOfName).Append(") ? ").Append(i)
                                .Append(" : ").Append(i + plan.TotalMemberCount).Append(",").NewLine();
                        }
                    }
                    sb.Append("_ => -1,").Outdent().Append(";").Outdent().NewLine();
                }
            }

            sb.Append("return null;").Outdent().NewLine();
        }
        void WriteReadMethod()
        {
            const string DeferredConstructionVariableName = "value";

            sb.Append("public override ").Append(plan.TypeName).Append(" Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)").Indent().NewLine();

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
                    if (member.IsMapped)
                    {
                        var variableName = DeferredConstructionVariableName + token;

                        if (member.CouldBeNullable) sb.Append(member.AnnotatedTypeName);
                        else sb.Append(member.TypeName);

                        sb.Append(' ').Append(variableName).Append(" = default")
                            // if "default" will violate NRT: add a !
                            .Append(member.NeedsDefaultBang ? "!" : "")
                            .Append(";").NewLine();

                        if (plan.UseConstructor && member.ConstructorParameterOrder is not null)
                        {
                            deferredMethodArgumentsOrdered.Add(member.ConstructorParameterOrder.Value, variableName);
                        }
                        else if (plan.UseFactoryMethod && member.FactoryMethodParameterOrder is not null)
                        {
                            deferredMethodArgumentsOrdered.Add(member.FactoryMethodParameterOrder.Value, variableName);
                        }
                    }
                    token++;
                }
            }
            else
            {
                // we are not using a constructor, so we need to create an instance now
                sb.Append(plan.NonNullTypeName).Append(" result = new();").NewLine();
            }

            if (!queryColumns.IsDefault && flags.HasAny(OperationFlags.StrictTypes))
            {
                // no mapping involved - simple ordinal iteration
                sb.Append("int lim = global::System.Math.Min(tokens.Length, ").Append(queryColumns.Length).Append(");").NewLine()
                    .Append("for (int token = 0; token < lim; token++) // query-columns predefined");
            }
            else
            {
                sb.Append("foreach (var token in tokens)");
            }
            sb.Indent().NewLine().Append("switch (token)").Indent().NewLine();

            token = 0;
            foreach (var member in members)
            {
                if (member.IsMapped)
                {
                    var nullCheck = member.CouldBeNullable ? $"reader.IsDBNull(columnOffset) ? ({member.AnnotatedTypeName})null : " : "";
                    sb.Append("case ").Append(token).Append(":").NewLine().Indent(false);

                    // write `result.X = ` or `member0 = `
                    if (useDeferredConstruction) sb.Append(DeferredConstructionVariableName).Append(token);
                    else sb.Append("result.").Append(member.CodeName);
                    sb.Append(" = ");

                    sb.Append(nullCheck);
                    if (member.ReaderMethod is null)
                    {
                        sb.Append("reader.GetFieldValue<").Append(member.TypeName).Append(">(columnOffset);");
                    }
                    else
                    {
                        sb.Append("reader.").Append(member.ReaderMethod).Append("(columnOffset);");
                    }


                    sb.NewLine().Append("break;").NewLine().Outdent(false);

                    // optionally emit type-forgiving version
                    if (!flags.HasAny(OperationFlags.StrictTypes))
                    {
                        sb.Append("case ").Append(token + plan.TotalMemberCount).Append(":").NewLine().Indent(false);

                        // write `result.X = ` or `member0 = `
                        if (useDeferredConstruction) sb.Append(DeferredConstructionVariableName).Append(token);
                        else sb.Append("result.").Append(member.CodeName);

                        sb.Append(" = ")
                            .Append(nullCheck)
                            .Append("GetValue<")
                            .Append(member.NonNullTypeName).Append(">(reader, columnOffset);").NewLine()
                            .Append("break;").NewLine().Outdent(false);
                    }
                }
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

                if (plan.UseConstructor)
                {
                    // `return new Type(member0, member1, member2, ...);`
                    sb.Append("return new ").Append(plan.TypeName).Append('(');
                    WriteDeferredMethodArgs();
                    sb.Append(')');
                    WriteDeferredInitialization();
                    sb.Append(";").Outdent();
                }
                else if (plan.UseFactoryMethod)
                {
                    // `return Type.FactoryCreate(member0, member1, member2, ...);`
                    sb.Append("return ").Append(plan.TypeName)
                      .Append('.').Append(plan.FactoryMethodName).Append('(');
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
                    sb.Append("return new ").Append(plan.TypeName);
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
                        if (member.IsMapped)
                        {
                            if (member.ConstructorParameterOrder is not null) continue; // already used in constructor arguments
                            sb.Append(member.CodeName).Append(" = ").Append(DeferredConstructionVariableName).Append(token).Append(',').NewLine();
                        }
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

    private static void WriteArgs(in GenerateState ctx, ParamPlan? parameterType, CodeWriter sb, WriteArgsMode mode, string map, ref WriteArgsFlags flags)
    {
        if (parameterType is null)
        {
            return;
        }

        var source = "args";

        if (parameterType.IsAnonymous)
        {
            sb.Append("var typed = Cast(args, ").Append(parameterType.ShapeLambda).Append("); // expected shape").NewLine();
            source = "typed";
        }

        if (mode == WriteArgsMode.Add)
        {   // we'll calculate this; assume we can, and claw backwards from there
            flags |= WriteArgsFlags.CanPrepare;
        }

        bool first = true, firstTest = true;
        int parameterIndex = 0;
        var planMembers = parameterType.Members;
        if (planMembers.IsEmpty) return;

        // Add mode uses a shared "p" local, declared at method scope because the per-member
        // Include(...) guards each open their own block; a factory whose members all expand
        // (PackListParameters) never touches it, and declaring it then is CS0168
        bool needsParameterLocal = false;
        if (mode == WriteArgsMode.Add)
        {
            foreach (var member in planMembers)
            {
                if (member.IsMapped && !member.IsCancellation && !member.IsRowCount
                    && !member.IsExpandable && !member.IsCustom
                    && SqlTools.IncludeParameter(map, member.CodeName, out _))
                {
                    needsParameterLocal = true;
                    break;
                }
            }
        }

        foreach (var member in planMembers)
        {
            if (!member.IsMapped) continue;

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
                if (needsParameterLocal)
                {
                    sb.Append("global::System.Data.Common.DbParameter p;").NewLine();
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
                    if (member.IsCustom)
                    {
                        // ICustomQueryParameter (TVPs etc): the value adds itself; it declares
                        // no DbType, so the command cannot be prepared. The null throw matches
                        // vanilla Dapper's, which has no way to name a parameter it never got.
                        flags &= ~WriteArgsFlags.CanPrepare;
                        if (!member.IsValueType)
                        {
                            sb.Append("if (").Append(source).Append(".").Append(member.CodeName)
                              .Append(" is null) throw new global::System.InvalidOperationException(\"Member '")
                              .Append(member.CodeName).Append("' is an ICustomQueryParameter and cannot be null\");").NewLine();
                        }
                        sb.Append(source).Append(".").Append(member.CodeName).Append(".AddParameter(cmd.Command!, ")
                          .AppendVerbatimLiteral(member.DbName).Append(");").NewLine();
                        break;
                    }
                    if (member.IsExpandable)
                    {
                        // list-expansion (where X in @ids): delegate to Dapper's own implementation,
                        // which owns the whole contract - SQL rewrite (including the empty-list and
                        // optimize-hint forms), per-item parameters, DbString items, padding and
                        // string_split settings, and provider array support
                        flags &= ~WriteArgsFlags.CanPrepare; // parameter shape varies by list size
                        sb.Append("#pragma warning disable CS0618 // list-expansion: this *is* the library usage").NewLine()
                          .Append("global::Dapper.SqlMapper.PackListParameters(cmd.Command!, ").AppendVerbatimLiteral(member.DbName)
                          .Append(", ").Append(source).Append(".").Append(member.CodeName).Append(");").NewLine()
                          .Append("#pragma warning restore CS0618").NewLine();
                        break;
                    }
                    sb.Append("p = cmd.CreateParameter();").NewLine();
                    sb.Append("p.ParameterName = ").AppendVerbatimLiteral(member.DbName).Append(";").NewLine();

                    if (member.IsDbString)
                    {
                        ctx.GeneratorContext.IncludeGenerationType(IncludedGeneration.DbStringHelpers);

                        sb.Append("global::Dapper.Aot.Generated.DbStringHelpers.ConfigureDbStringDbParameter(p, ")
                          .Append(source).Append(".").Append(member.DbName).Append(");").NewLine();

                        sb.Append("ps.Add(p);").NewLine(); // dont forget to add parameter to command parameters collection
                        break;
                    }

                    bool useSetValueWithDefaultSize = member.UseSetValueWithDefaultSize;
                    if (member.HasDbType)
                    {
                        sb.Append("p.DbType = global::System.Data.DbType.").Append(member.DbTypeName).Append(";").NewLine();
                    }
                    else
                    {
                        // prepare requires all args to have a type (it also requires all
                        // string/binary args to have a size, but: we've set that)
                        flags &= ~WriteArgsFlags.CanPrepare;
                    }
                    AppendDbParameterSetting(sb, "Size", member.EffectiveSize);
                    AppendDbParameterSetting(sb, "Precision", member.Precision);
                    AppendDbParameterSetting(sb, "Scale", member.Scale);

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
                    if (member.IsExpandable || member.IsCustom)
                    {
                        // update is only reachable via command-cache reuse and batch, both of
                        // which are refused for self-binding members at parse
                        break;
                    }
                    if (member.IsDbString)
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
                        .Append(member.TypeName).Append(">(ps[");
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
        // note: the incremental driver caches these values per node and decides re-runs by
        // equality, so every subclass must provide *structural* equality (see ModelShapeTests)
        public LocationSnapshot Location { get; }
        protected SourceState(in LocationSnapshot location) => Location = location;
    }

    internal sealed class SkippedSourceState : SourceState
    {
        // a call-site that Dapper.AOT is *not* handling - either the API is not supported at
        // all, or diagnostics made us leave it alone; retained so the DAP000 scorecard can
        // count honestly rather than quietly shrinking the denominator
        public OperationFlags Flags { get; }
        public SkippedSourceState(in LocationSnapshot location, OperationFlags flags) : base(location)
            => Flags = flags;

        public bool Equals(SkippedSourceState? other) => other is not null
            && Location.Equals(other.Location) && Flags == other.Flags;
        public override bool Equals(object? obj) => Equals(obj as SkippedSourceState);
        public override int GetHashCode() => Location.GetHashCode() ^ (int)Flags;
    }

    internal sealed class FaultSourceState : SourceState
    {
        public Exception Fault { get; }

        public FaultSourceState(in LocationSnapshot location, Exception fault) : base(location)
            => Fault = fault;

        public bool Equals(FaultSourceState? other) => other is not null
            && Location.Equals(other.Location)
            && Fault.GetType() == other.Fault.GetType()
            && string.Equals(Fault.Message, other.Fault.Message, StringComparison.Ordinal);
        public override bool Equals(object? obj) => Equals(obj as FaultSourceState);
        public override int GetHashCode() => Location.GetHashCode();
    }

    internal sealed class SuccessSourceState : SourceState
    {
        public string InterceptorFilePath { get; } // normalized per the interceptors spec
        public int LanguageVersion { get; } // raw LanguageVersion value; -1 when not C#

        public OperationFlags Flags { get; }
        public string? Sql { get; }
        public string ParameterMap { get; }
        public InterceptedMethod Method { get; }
        public RowPlan? ResultPlan { get; }
        public ParamPlan? ParameterPlan { get; }
        public AdditionalCommandState? AdditionalCommandState { get; }

        public SuccessSourceState(in LocationSnapshot location, string interceptorFilePath, int languageVersion,
            InterceptedMethod method, OperationFlags flags, string? sql,
            RowPlan? resultPlan, ParamPlan? parameterPlan, string parameterMap,
            AdditionalCommandState? additionalCommandState) : base(location)
        {
            InterceptorFilePath = interceptorFilePath;
            LanguageVersion = languageVersion;
            Flags = flags;
            Sql = sql;
            ResultPlan = resultPlan;
            ParameterPlan = parameterPlan;
            Method = method;
            ParameterMap = parameterMap;
            AdditionalCommandState = additionalCommandState;
        }

        public (OperationFlags Flags, InterceptedMethod Method, ParamPlan? ParameterPlan, string ParameterMap, LocationSnapshot? UniqueLocation, AdditionalCommandState? AdditionalCommandState) Group()
            => new(Flags, Method, ParameterPlan, ParameterMap, (Flags & (OperationFlags.CacheCommand | OperationFlags.IncludeLocation)) == 0 ? null : Location, AdditionalCommandState);

        public bool Equals(SuccessSourceState? other) => other is not null
            && Location.Equals(other.Location)
            && string.Equals(InterceptorFilePath, other.InterceptorFilePath, StringComparison.Ordinal)
            && LanguageVersion == other.LanguageVersion
            && Flags == other.Flags
            && string.Equals(Sql, other.Sql, StringComparison.Ordinal)
            && string.Equals(ParameterMap, other.ParameterMap, StringComparison.Ordinal)
            && Method.Equals(other.Method)
            && Equals(ResultPlan, other.ResultPlan)
            && Equals(ParameterPlan, other.ParameterPlan)
            && Equals(AdditionalCommandState, other.AdditionalCommandState);
        public override bool Equals(object? obj) => Equals(obj as SuccessSourceState);
        public override int GetHashCode()
        {
            var hash = Location.GetHashCode();
            hash = (hash * -47) + (int)Flags;
            hash = (hash * -47) + Method.GetHashCode();
            return hash;
        }
    }
    private sealed class CommonComparer :
        IComparer<LocationSnapshot>,
        IEqualityComparer<(OperationFlags Flags, InterceptedMethod Method, ParamPlan? ParameterPlan, string ParameterMap, LocationSnapshot? UniqueLocation, AdditionalCommandState? AdditionalCommandState)>
    {
        public static readonly CommonComparer Instance = new();
        private CommonComparer() { }

        public int Compare(LocationSnapshot x, LocationSnapshot y)
        {
            // same semantics as the old Location-based LocationComparer: path, then start, then end
            var delta = StringComparer.InvariantCulture.Compare(x.Path, y.Path);
            if (delta == 0)
            {
                delta = (x.StartLine, x.StartChar).CompareTo((y.StartLine, y.StartChar));
            }
            if (delta == 0)
            {
                delta = (x.EndLine, x.EndChar).CompareTo((y.EndLine, y.EndChar));
            }
            return delta;
        }

        public bool Equals(

            (OperationFlags Flags, InterceptedMethod Method, ParamPlan? ParameterPlan, string ParameterMap, LocationSnapshot? UniqueLocation, AdditionalCommandState? AdditionalCommandState) x,
            (OperationFlags Flags, InterceptedMethod Method, ParamPlan? ParameterPlan, string ParameterMap, LocationSnapshot? UniqueLocation, AdditionalCommandState? AdditionalCommandState) y) => x.Flags == y.Flags
                && x.ParameterMap == y.ParameterMap
                && x.Method.Equals(y.Method)
                && Equals(x.ParameterPlan, y.ParameterPlan)
                && Nullable.Equals(x.UniqueLocation, y.UniqueLocation)
                && Equals(x.AdditionalCommandState, y.AdditionalCommandState);

        public int GetHashCode((OperationFlags Flags, InterceptedMethod Method, ParamPlan? ParameterPlan, string ParameterMap, LocationSnapshot? UniqueLocation, AdditionalCommandState? AdditionalCommandState) obj)
        {
            var hash = (int)obj.Flags;
            hash *= -47;
            hash += obj.ParameterMap.GetHashCode();
            hash *= -47;
            hash += obj.Method.GetHashCode();
            hash *= -47;
            if (obj.ParameterPlan is not null)
            {
                hash += obj.ParameterPlan.GetHashCode();
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
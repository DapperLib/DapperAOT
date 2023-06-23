using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dapper.CodeAnalysis;

/// <summary>
/// Analyses source for Dapper syntax and generates suitable interceptors where possible.
/// </summary>
[Generator(LanguageNames.CSharp), DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class DapperInterceptorGenerator : DiagnosticAnalyzer, IIncrementalGenerator
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        // we won't register anything; all we really want here is to report our supported diagnostics
    }

    /// <summary>
    /// Whether to emit interceptors even if the "interceptors" feature is not detected
    /// </summary>
    public bool OverrideFeatureEnabled { get; set; }

    /// <summary>
    /// Provide log feedback.
    /// </summary>
    public event Action<DiagnosticSeverity, string>? Log;

    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                    .Where(x => x is not null)
                    .Select((x, _) => x!);
        var combined = context.CompilationProvider.Combine(nodes.Collect());
        context.RegisterImplementationSourceOutput(combined, Generate);
    }

    // very fast and light-weight; we'll worry about the rest later from the semantic tree
    internal static bool IsCandidate(string methodName) => methodName.StartsWith("Execute") || methodName.StartsWith("Query");

    private bool PreFilter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            return IsCandidate(ma.Name.ToString());
        }

        return false;
    }

    private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.Node is not InvocationExpressionSyntax ie
            || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op)
        {
            return null;
        }
        var methodKind = IsSupportedDapperMethod(op, out var flags);
        switch (methodKind)
        {
            case DapperMethodKind.DapperUnsupported:
                flags |= OperationFlags.DoNotGenerate;
                break;
            case DapperMethodKind.DapperSupported:
                break;
            default: // includes NotDapper
                return null;
        }

        // everything requires location
        Location? loc = null;

        if (op.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            loc = ma.ChildNodes().Skip(1).FirstOrDefault()?.GetLocation();
        }
        loc ??= op.Syntax.GetLocation();
        if (loc is null)
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"No location found; cannot intercept");
            return null;
        }

        // check whether we can use this method
        object? diagnostics = null;
        if (Inspection.IsEnabled(ctx, op, Types.DapperAotAttribute, out _, cancellationToken))
        {
            if (methodKind == DapperMethodKind.DapperUnsupported)
            {
                flags |= OperationFlags.DoNotGenerate;
                Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.UnsupportedMethod, loc, GetSignature(op.TargetMethod)));
            }
        }
        else
        {
            // no diagnostic per-item; the generator will emit a single diagnostic if everything is disabled
            flags |= OperationFlags.AotNotEnabled | OperationFlags.DoNotGenerate;
            // but we still process the other bits, as there might be relevant additional things
        }

        if (Inspection.IsEnabled(ctx, op, Types.CacheCommandAttribute, out _, cancellationToken))
        {
            flags |= OperationFlags.CacheCommand;
        }

        Log?.Invoke(DiagnosticSeverity.Hidden, $"Found {op.TargetMethod.Name}: {flags} at {loc}");

        ITypeSymbol? resultType = null, paramType = null;
        if (HasAny(flags, OperationFlags.TypedResult))
        {
            var typeArgs = op.TargetMethod.TypeArguments;
            if (typeArgs.Length == 1)
            {
                resultType = typeArgs[0];
            }
        }

        string? sql = null;
        Location? sqlLocation = null;
        bool? buffered = null;
        if (!HasAny(flags, OperationFlags.DoNotGenerate))
        {
            foreach (var arg in op.Arguments)
            {
                switch (arg.Parameter?.Name)
                {
                    case "sql":
                        if (TryGetConstantValueWithLocation(arg, out string? s, out sqlLocation))
                        {
                            sql = s;
                        }
                        break;
                    case "buffered":
                        if (TryGetConstantValue(arg, out bool b))
                        {
                            buffered = b;
                        }
                        break;
                    case "param":
                        if (arg.Value is not IDefaultValueOperation)
                        {
                            var expr = arg.Value;
                            if (expr is IConversionOperation conv && expr.Type?.SpecialType == SpecialType.System_Object)
                            {
                                expr = conv.Operand;
                            }
                            paramType = expr?.Type;
                            flags |= OperationFlags.HasParameters;
                        }
                        break;
                    case "cnn":
                    case "commandTimeout":
                    case "transaction":
                        // nothing to do
                        break;
                    case "commandType":
                        if (TryGetConstantValue(arg, out int? ct))
                        {
                            switch (ct)
                            {
                                case null when !string.IsNullOrWhiteSpace(sql):
                                    // if no spaces: interpret as stored proc, else: text
                                    flags |= sql!.Trim().IndexOf(' ') < 0 ? OperationFlags.StoredProcedure : OperationFlags.Text;
                                    break;
                                case null:
                                    break; // flexible
                                case 1:
                                    flags |= OperationFlags.Text;
                                    break;
                                case 4:
                                    flags |= OperationFlags.StoredProcedure;
                                    break;
                                case 512:
                                    flags |= OperationFlags.TableDirect;
                                    break;
                                default:
                                    flags |= OperationFlags.DoNotGenerate;
                                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.UnexpectedCommandType, arg.Syntax.GetLocation()));
                                    break;
                            }
                        }
                        break;
                    default:
                        flags |= OperationFlags.DoNotGenerate;
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.UnexpectedArgument, arg.Syntax.GetLocation(), arg.Parameter?.Name));
                        break;
                }
            }
        }
        if (HasAny(flags, OperationFlags.Query) && buffered.HasValue)
        {
            flags |= buffered.GetValueOrDefault() ? OperationFlags.Buffered : OperationFlags.Unbuffered;
        }

        // additional result-type checks
        if (HasAny(flags, OperationFlags.Query) || HasAll(flags, OperationFlags.Execute | OperationFlags.Scalar))
        {
            bool resultTuple = Inspection.InvolvesTupleType(resultType, out _);
            if (HasAny(flags, OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (resultTuple && Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out _, cancellationToken))
                {   // Dapper vanilla supports bind-by-position for tuples; warn if bind-by-name is enabled
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperLegacyBindNameTupleResults, loc));
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (Inspection.InvolvesGenericTypeParameter(resultType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.GenericTypeParameter, loc, resultType!.ToDisplayString()));
                }
                else if (resultTuple)
                {
                    if (Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out var defined, cancellationToken))
                    {
                        flags |= OperationFlags.BindTupleResultByName;
                    }
                    if (!defined)
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperAotAddBindTupleByName, loc));
                    }

                    // but not implemented currently!
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperAotTupleResults, loc));
                }
                else if (!Inspection.IsPublicOrAssemblyLocal(resultType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.NonPublicType, loc, failing!.ToDisplayString(), Inspection.NameAccessibility(failing)));
                }
            }
        }

        // additional parameter checks
        if (HasAny(flags, OperationFlags.HasParameters))
        {
            bool paramTuple = Inspection.InvolvesTupleType(paramType, out _);
            if (HasAny(flags, OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (paramTuple)
                {
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperLegacyTupleParameter, loc));
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (paramTuple)
                {
                    if (Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out var defined, cancellationToken))
                    {
                        flags |= OperationFlags.BindTupleParameterByName;
                    }
                    if (!defined)
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperAotAddBindTupleByName, loc));
                    }

                    // but not implemented currently!
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DapperAotTupleParameter, loc));
                }
                else if (Inspection.InvolvesGenericTypeParameter(paramType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.GenericTypeParameter, loc, paramType!.ToDisplayString()));
                }
                else if (Inspection.IsMissingOrObjectOrDynamic(paramType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.UntypedParameter, loc));
                }
                else if (!Inspection.IsPublicOrAssemblyLocal(paramType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.NonPublicType, loc, failing!.ToDisplayString(), Inspection.NameAccessibility(failing)));
                }
            }
        }

        var parameterMap = BuildParameterMap(op, sql, flags, paramType, loc, ref diagnostics, sqlLocation, out var parseFlags);

        // if we have a good parser *and* the SQL isn't borked: check for obvious query/exec mismatch
        if ((parseFlags & (ParseFlags.Reliable | ParseFlags.SyntaxError)) == ParseFlags.Reliable)
        {
            switch (flags & (OperationFlags.Execute | OperationFlags.Query | OperationFlags.Scalar))
            {
                case OperationFlags.Execute:
                    if ((parseFlags & ParseFlags.Query) != 0)
                    {
                        // definitely have a query
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.ExecuteCommandWithQuery, loc));
                    }
                    break;
                case OperationFlags.Query:
                case OperationFlags.Execute | OperationFlags.Scalar:
                    if ((parseFlags & (ParseFlags.Query | ParseFlags.MaybeQuery)) == 0)
                    {
                        // definitely do not have a query
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.QueryCommandMissingQuery, loc));
                    }
                    break;
            }
        }

        if (HasAny(flags, OperationFlags.CacheCommand))
        {
            bool canBeCached = true;
            // need fixed text, command-type and parameters to be reusable
            if (string.IsNullOrWhiteSpace(sql) || parameterMap == "?" || !HasAny(flags, OperationFlags.StoredProcedure | OperationFlags.TableDirect | OperationFlags.Text))
            {
                canBeCached = false;
            }

            if (!canBeCached) flags &= ~OperationFlags.CacheCommand;
        }
        return new SourceState(loc, op.TargetMethod, flags, sql, resultType, paramType, parameterMap, diagnostics);

        //static bool HasDiagnostic(object? diagnostics, DiagnosticDescriptor diagnostic)
        //{
        //    if (diagnostic is null) throw new ArgumentNullException(nameof(diagnostic));
        //    switch (diagnostics)
        //    {
        //        case null: return false;
        //        case Diagnostic single: return single.Descriptor == diagnostic;
        //        case IEnumerable<Diagnostic> list:
        //            foreach (var single in list)
        //            {
        //                if (single.Descriptor == diagnostic) return true;
        //            }
        //            return false;
        //        default: throw new ArgumentException(nameof(diagnostics));
        //    }
        //}

        static bool TryGetConstantValue<T>(IArgumentOperation op, out T? value)
            => TryGetConstantValueWithLocation<T>(op, out value, out _);

        static bool TryGetConstantValueWithLocation<T>(IArgumentOperation op, out T? value, out Location? location)
        {
            try
            {
                if (op.ConstantValue.HasValue)
                {
                    value = (T?)op.ConstantValue.Value;
                    location = op.Syntax.GetLocation();
                    return true;
                }
                var val = op.Value;
                if (val is IConversionOperation conv)
                {
                    val = conv.Operand;
                }

                if (val is IFieldReferenceOperation field && field.Field.HasConstantValue)
                {
                    value = (T?)field.Field.ConstantValue;
                    location = field.Field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.GetLocation();
                    return true;
                }

                if (val is ILiteralOperation or IDefaultValueOperation)
                {
                    var v = val.ConstantValue;
                    value = v.HasValue ? (T?)v.Value : default;
                    location = val.Syntax.GetLocation();
                    return true;
                }
            }
            catch { }
            value = default!;
            location = null;
            return false;
        }


        static string BuildParameterMap(IInvocationOperation op, string? sql, OperationFlags flags, ITypeSymbol? parameterType, Location loc, ref object? diagnostics, Location? sqlLocation, out ParseFlags parseFlags)
        {
            if (HasAny(flags, OperationFlags.DoNotGenerate))
            {
                parseFlags = ParseFlags.MaybeQuery;
                return "";
            }
            // if command-type is known statically to be stored procedure etc: pass everything
            if (HasAny(flags, OperationFlags.StoredProcedure | OperationFlags.TableDirect))
            {
                parseFlags = HasAny(flags, OperationFlags.StoredProcedure) ? ParseFlags.MaybeQuery : ParseFlags.Query;
                return HasAny(flags, OperationFlags.HasParameters) ? "*" : "";
            }
            // if command-type or command is not known statically: defer decision
            if (!HasAny(flags, OperationFlags.Text) || string.IsNullOrWhiteSpace(sql))
            {
                parseFlags = ParseFlags.MaybeQuery;
                return HasAny(flags, OperationFlags.HasParameters) ? "?" : "";
            }

            // so: we know statically that we have known command-text
            // first, try try to find any parameters
            ImmutableHashSet<string> paramNames;
            switch (IdentifySqlSyntax(op, out bool caseSensitive))
            {
                case SqlSyntax.TransactSql:
                    var proc = new DiagnosticTSqlProcessor(caseSensitive, diagnostics, loc, sqlLocation);
                    try
                    {
                        proc.Execute(sql!);
                        parseFlags = proc.Flags;
                        paramNames = (from var in proc.Variables
                                      where var.IsParameter
                                      select var.Name.StartsWith("@") ? var.Name.Substring(1) : var.Name
                                      ).ToImmutableHashSet();
                        diagnostics = proc.DiagnosticsObject;
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.SqlError, loc, ex.Message));
                        goto default; // some internal failure
                    }
                    break;
                default:
                    paramNames = SqlTools.GetUniqueParameters(sql, out parseFlags);
                    break;
            }

            if (paramNames.IsEmpty && (parseFlags & ParseFlags.Return) == 0) // return is a parameter, sort of
            {
                if (HasAny(flags, OperationFlags.HasParameters))
                {
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.SqlParametersNotDetected, loc));
                }
                return "";
            }

            // so, we definitely detect parameters (note: don't warn just for return)
            if (!HasAny(flags, OperationFlags.HasParameters) && !paramNames.IsEmpty)
            {
                Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.NoParametersSupplied, loc));
                return "";
            }
            if (parameterType is null)
            {
                // unknown parameter type; defer decision
                return "?";
            }

            // ok, so the SQL is using parameters; check what we have
            var memberDbToCodeNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            if (!Inspection.IsCollectionType(parameterType, out var elementType))
            {
                elementType = parameterType;
            }
            if (elementType is null)
            {
                return "";
            }

            string? returnCodeMember = null, rowCountMember = null;
            foreach (var member in Inspection.GetMembers(elementType))
            {
                if (member.IsRowCount)
                {
                    if (rowCountMember is null)
                    {
                        rowCountMember = member.CodeName;
                    }
                    else
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DuplicateRowCount, loc, member.CodeName, rowCountMember));
                    }
                    if (member.HasDbValueAttribute)
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.RowCountDbValue, loc, member.CodeName));
                    }
                    continue; // not treated as parameters for naming etc purposes
                }
                var dbName = member.DbName;
                if (memberDbToCodeNames.TryGetValue(dbName, out var existing))
                {
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DuplicateParameter, loc, member.CodeName, existing, dbName));
                }
                else
                {
                    memberDbToCodeNames.Add(dbName, member.CodeName);
                }
                if (member.Direction == ParameterDirection.ReturnValue)
                {
                    if (returnCodeMember is null)
                    {
                        returnCodeMember = member.CodeName;
                    }
                    else
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.DuplicateReturn, loc, member.CodeName, returnCodeMember));
                    }
                }
            }

            StringBuilder? sb = null;
            foreach (var sqlParamName in paramNames.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase))
            {
                if (memberDbToCodeNames.TryGetValue(sqlParamName, out var codeName))
                {
                    WithSpace(ref sb).Append(codeName);
                }
                else
                {
                    // we can only consider this an error if we're confident in how well we parsed the input
                    if ((parseFlags & ParseFlags.Reliable) != 0)
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.SqlParameterNotBound, loc, sqlParamName, CodeWriter.GetTypeName(elementType)));
                    }
                }
            }
            if ((parseFlags & ParseFlags.Return) != 0 && returnCodeMember is not null)
            {
                WithSpace(ref sb).Append(returnCodeMember);
            }
            return sb is null ? "" : sb.ToString();

            static StringBuilder WithSpace(ref StringBuilder? sb) => sb is null ? (sb = new()) : (sb.Length == 0 ? sb : sb.Append(' '));
        }
    }

    private enum SqlSyntax
    {
        General = 0,
        TransactSql = 1,
    }
    private static SqlSyntax IdentifySqlSyntax(IInvocationOperation op, out bool caseSensitive)
    {
        caseSensitive = false;
        if (op.Arguments[0].Value is IConversionOperation conv && conv.Operand.Type is INamedTypeSymbol { Arity: 0 } type)
        {
            if (type is
                {
                    Name: "SqlConnection", ContainingType: null,
                    ContainingNamespace:
                    {
                        Name: "SqlClient",
                        ContainingNamespace:
                        {
                            Name: "Data",
                            ContainingNamespace:
                            {
                                Name: "System" or "Microsoft",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    }
                })
            {
                return SqlSyntax.TransactSql;
            }
        }

        // TODO : attribute mechanism

        return SqlSyntax.General;
    }

    enum DapperMethodKind
    {
        NotDapper,
        DapperUnsupported,
        DapperSupported,
    }

    static DapperMethodKind IsSupportedDapperMethod(IInvocationOperation operation, out OperationFlags flags)
    {
        flags = OperationFlags.None;
        var method = operation?.TargetMethod;
        if (method is null || !method.IsExtensionMethod)
        {
            return DapperMethodKind.NotDapper;
        }
        var type = method.ContainingType;
        if (type is not { Name: "SqlMapper", ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
        {
            return DapperMethodKind.NotDapper;
        }
        if (method.Name.EndsWith("Async"))
        {
            flags |= OperationFlags.Async;
        }
        if (method.IsGenericMethod)
        {
            flags |= OperationFlags.TypedResult;
        }
        switch (method.Name)
        {
            case "Query":
                flags |= OperationFlags.Query;
                return method.Arity <= 1 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            case "QueryAsync":
            case "QueryUnbufferedAsync":
                flags |= method.Name.Contains("Unbuffered") ? OperationFlags.Unbuffered : OperationFlags.Buffered;
                goto case "Query";
            case "QueryFirst":
            case "QueryFirstAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne;
                goto case "Query";
            case "QueryFirstOrDefault":
            case "QueryFirstOrDefaultAsync":
                flags |= OperationFlags.SingleRow;
                goto case "Query";
            case "QuerySingle":
            case "QuerySingleAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne;
                goto case "Query";
            case "QuerySingleOrDefault":
            case "QuerySingleOrDefaultAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtMostOne;
                goto case "Query";
            case "Execute":
            case "ExecuteAsync":
                flags |= OperationFlags.Execute;
                return method.Arity == 0 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            case "ExecuteScalar":
            case "ExecuteScalarAsync":
                flags |= OperationFlags.Execute | OperationFlags.Scalar;
                return method.Arity <= 1 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            default:
                return DapperMethodKind.DapperUnsupported;
        }
    }

    static bool HasAny(OperationFlags value, OperationFlags testFor) => (value & testFor) != 0;
    static bool HasAll(OperationFlags value, OperationFlags testFor) => (value & testFor) == testFor;

    [Flags]
    enum OperationFlags
    {
        None = 0,
        Query = 1 << 0,
        Execute = 1 << 1,
        Async = 1 << 2,
        TypedResult = 1 << 3,
        HasParameters = 1 << 4,
        Buffered = 1 << 5,
        Unbuffered = 1 << 6,
        SingleRow = 1 << 7,
        Text = 1 << 8,
        StoredProcedure = 1 << 9,
        TableDirect = 1 << 10,
        AtLeastOne = 1 << 11,
        AtMostOne = 1 << 12,
        Scalar = 1 << 13,
        DoNotGenerate = 1 << 14,
        AotNotEnabled = 1 << 15,
        BindTupleResultByName = 1 << 16,
        BindTupleParameterByName = 1 << 17,
        CacheCommand = 1 << 18,
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

    private static string GetSignature(IMethodSymbol method, bool deconstruct = true)
    {
        if (deconstruct && method.IsGenericMethod) method = method.ConstructedFrom ?? method;
        return method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
    }

    private bool CheckPrerequisites(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state, out int enabledCount)
    {
        enabledCount = 0;
        if (!state.Nodes.IsDefaultOrEmpty)
        {
            int errorCount = 0, disabledCount = 0, doNotGenerateCount = 0;
            bool checkParseOptions = true;
            foreach (var node in state.Nodes)
            {
                var count = node.DiagnosticCount;
                for (int i = 0; i < count; i++)
                {
                    ctx.ReportDiagnostic(node.GetDiagnostic(i));
                }

                if (HasAny(node.Flags, OperationFlags.DoNotGenerate)) doNotGenerateCount++;

                if ((node.Flags & OperationFlags.AotNotEnabled) != 0)
                {
                    disabledCount++;
                }
                else
                {
                    enabledCount++;
                    // find the first enabled thing with a C# parse options
                    if (checkParseOptions && node.Location.SourceTree?.Options is CSharpParseOptions options)
                    {
                        checkParseOptions = false; // only do this once
                        if (!(OverrideFeatureEnabled || options.Features.ContainsKey("InterceptorsPreview")))
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InterceptorsNotEnabled, null));
                            errorCount++;
                        }

                        var version = options.LanguageVersion;
                        if (version != LanguageVersion.Default && version < LanguageVersion.CSharp11)
                        {
                            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.LanguageVersionTooLow, null));
                            errorCount++;
                        }
                    }
                }
            }
            if (disabledCount == state.Nodes.Length)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.DapperAotNotEnabled, null));
                errorCount++;
            }

            if (doNotGenerateCount == state.Nodes.Length)
            {
                // nothing but nope
                errorCount++;
            }

            return errorCount == 0;
        }
        return false; // nothing to validate - so: nothing to do, quick exit
    }

    private void Generate(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        if (!CheckPrerequisites(ctx, state, out int enabledCount)) // also reports per-item diagnostics
        {
            // failed checks; do nothing
            return;
        }

        var dbCommandTypes = IdentifyDbCommandTypes(state.Compilation, out var needsCommandPrep);

        bool allowUnsafe = state.Compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;
        var sb = new CodeWriter().Append("#nullable enable").NewLine()
            .Append("file static class DapperGeneratedInterceptors").Indent().NewLine();
        int methodIndex = 0, callSiteCount = 0;

        var factories = new CommandFactoryState(state.Compilation);
        var readers = new RowReaderState();

        foreach (var grp in state.Nodes.Where(x => !HasAny(x.Flags, OperationFlags.DoNotGenerate)).GroupBy(x => x.Group(), CommonComparer.Instance))
        {
            // first, try to resolve the helper method that we're going to use for this
            var (flags, method, parameterType, parameterMap, _) = grp.Key;
            int arity = HasAny(flags, OperationFlags.TypedResult) ? 2 : 1, argCount = 8;
            const bool useUnsafe = false;
            var helperName = method.Name;
            if (helperName == "Query")
            {
                if (HasAny(flags, OperationFlags.Buffered | OperationFlags.Unbuffered))
                {
                    //dedicated mode
                    helperName = HasAny(flags, OperationFlags.Buffered) ? "QueryBuffered" : "QueryUnbuffered";
                }
                else
                {
                    // fallback mode, needs an extra arg to pass in "buffered"
                    argCount++;
                }
            }

            int usageCount = 0;

            foreach (var op in grp.OrderBy(row => row.Location, CommonComparer.Instance))
            {
                var loc = op.Location.GetLineSpan();
                var start = loc.StartLinePosition;
                sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                    .AppendVerbatimLiteral(loc.Path).Append(", ").Append(start.Line + 1).Append(", ").Append(start.Character + 1).Append(")]").NewLine();
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
            if (HasAny(flags, OperationFlags.HasParameters))
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
            if (HasAny(flags, OperationFlags.TypedResult))
            {
                resultType = grp.First().ResultType!;
                sb.Append("// returns data: ").Append(resultType).NewLine();
            }

            // assertions
            var commandTypeMode = flags & (OperationFlags.Text | OperationFlags.StoredProcedure | OperationFlags.TableDirect);
            var methodParameters = grp.Key.Method.Parameters;
            sb.Append("global::System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(sql));").NewLine();
            if (HasParam(methodParameters, "commandType"))
            {
                if (commandTypeMode != 0)
                {
                    sb.Append("global::System.Diagnostics.Debug.Assert((commandType ?? global::Dapper.DapperAotExtensions.GetCommandType(sql)) == global::System.Data.CommandType.")
                            .Append(commandTypeMode.ToString()).Append(");").NewLine();
                }
            }

            if (HasAny(flags, OperationFlags.Buffered | OperationFlags.Unbuffered) && HasParam(methodParameters, "buffered"))
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(buffered is ").Append((flags & OperationFlags.Buffered) != 0).Append(");").NewLine();
            }

            sb.Append("global::System.Diagnostics.Debug.Assert(param is ").Append(HasAny(flags, OperationFlags.HasParameters) ? "not " : "").Append("null);").NewLine().NewLine();

            if (!TryWriteMultiExecImplementation(sb, flags, commandTypeMode, parameterType, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories))
            {
                WriteSingleImplementation(sb, method, resultType, flags, commandTypeMode, parameterType, grp.Key.ParameterMap, grp.Key.UniqueLocation is not null, methodParameters, factories, readers);
            }
        }

        const string DapperBaseCommandFactory = "global::Dapper.CommandFactory";
        var baseCommandFactory = GetCommandFactory(state.Compilation, out var canConstruct) ?? DapperBaseCommandFactory;
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
            WriteRowFactory(sb, pair.Type, pair.Index);
        }

        foreach (var tuple in factories)
        {
            WriteCommandFactory(baseCommandFactory, sb, tuple.Type, tuple.Index, tuple.Map, tuple.CacheCount);
        }

        sb.Outdent(); // ends our generated file-scoped class

        // we need an accessible [InterceptsLocation] - if not; add our own in the generated code
        var attrib = state.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InterceptsLocationAttribute");
        if (attrib is null || attrib.DeclaredAccessibility != Accessibility.Public)
        {
            sb.NewLine().Append(Resources.ReadString("Dapper.InterceptsLocationAttribute.cs"));
        }
        ctx.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.InterceptorsGenerated, null, callSiteCount, enabledCount, methodIndex, factories.Count(), readers.Count()));
    }

    private static void WriteCommandFactory(string baseFactory, CodeWriter sb, ITypeSymbol type, int index, string map, int cacheCount)
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

        var flags = WriteArgsFlags.None;
        if (string.IsNullOrWhiteSpace(map))
        {
            flags = WriteArgsFlags.CanPrepare;
        }
        else
        {
            sb.Append("public override void AddParameters(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine();
            WriteArgs(type, sb, WriteArgsMode.Add, map, ref flags);
            sb.Outdent().NewLine();

            sb.Append("public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine();
            WriteArgs(type, sb, WriteArgsMode.Update, map, ref flags);
            sb.Outdent().NewLine();


            if ((flags & WriteArgsFlags.NeedsPostProcess) != 0)
            {
                sb.Append("public override void PostProcess(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine();
                WriteArgs(type, sb, WriteArgsMode.PostProcess, map, ref flags);
                sb.Outdent().NewLine();
            }

            if ((flags & WriteArgsFlags.NeedsRowCount) != 0)
            {
                sb.Append("public override void PostProcess(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args, int rowCount)").Indent().NewLine();
                WriteArgs(type, sb, WriteArgsMode.SetRowCount, map, ref flags);
                sb.Append("PostProcess(cmd, args);");
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
            sb.NewLine().Append("public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection,").Indent(false).NewLine()
                .Append("string sql, global::System.Data.CommandType commandType, ")
                .Append(declaredType).Append(" args)").NewLine()
                .Append(" => TryReuse(ref Storage, sql, commandType, args) ?? base.GetCommand(connection, sql, commandType, args);").Outdent(false)
                .NewLine().NewLine().Append("public override bool TryRecycle(global::System.Data.Common.DbCommand command) => TryRecycle(ref Storage, command);").NewLine();

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

        sb.Outdent().NewLine().NewLine();
    }

    private static void WriteRowFactory(CodeWriter sb, ITypeSymbol type, int index)
    {
        var members = Inspection.GetMembers(type).ToImmutableArray();
        var memberCount = 0;
        foreach (var member in members)
        {
            if (CodeWriter.IsSettableInstanceMember(member.Member, out _))
            {
                memberCount++;
            }
        }

        sb.Append("private sealed class RowFactory").Append(index).Append(" : global::Dapper.RowFactory").Append("<").Append(type).Append(">")
            .Indent().NewLine()
            .Append("internal static readonly RowFactory").Append(index).Append(" Instance = new();").NewLine()
            .Append("private RowFactory").Append(index).Append("() {}").NewLine();

        if (memberCount != 0)
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
                if (CodeWriter.IsSettableInstanceMember(member.Member, out var memberType))
                {
                    var dbName = member.DbName;
                    sb.Append("case ").Append(StringHashing.NormalizedHash(dbName))
                        .Append(" when NormalizedEquals(name, ")
                        .AppendVerbatimLiteral(StringHashing.Normalize(dbName)).Append("):").Indent(false).NewLine()
                        .Append("token = type == typeof(").Append(Inspection.MakeNonNullable(memberType)).Append(") ? ").Append(token)
                        .Append(" : ").Append(token + memberCount).Append(";")
                        .Append(token == 0 ? " // two tokens for right-typed and type-flexible" : "").NewLine()
                        .Append("break;").Outdent(false).NewLine();
                    token++;
                }
            }
            sb.Outdent().NewLine()
                .Append("tokens[i] = token;").NewLine()
                .Append("columnOffset++;").NewLine();
            sb.Outdent().NewLine().Append("return null;").Outdent().NewLine();
        }
        sb.Append("public override ").Append(type).Append(" Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset, object? state)").Indent().NewLine();

        sb.Append(type.NullableAnnotation == NullableAnnotation.Annotated
            ? type.WithNullableAnnotation(NullableAnnotation.None) : type).Append(" result = new();").NewLine();

        if (memberCount != 0)
        {
            sb.Append("foreach (var token in tokens)").Indent().NewLine()
            .Append("switch (token)").Indent().NewLine();

            int token = 0;
            foreach (var member in members)
            {
                if (CodeWriter.IsSettableInstanceMember(member.Member, out var memberType))
                {
                    member.GetDbType(out var readerMethod);
                    var nullCheck = CouldBeNullable(memberType) ? $"reader.IsDBNull(columnOffset) ? ({CodeWriter.GetTypeName(memberType.WithNullableAnnotation(NullableAnnotation.Annotated))})null : " : "";
                    sb.Append("case ").Append(token).Append(":").NewLine().Indent(false)
                        .Append("result.").Append(member.CodeName).Append(" = ").Append(nullCheck);

                    if (readerMethod is null)
                    {
                        sb.Append("reader.GetFieldValue<").Append(memberType).Append(">(columnOffset);");
                    }
                    else
                    {
                        sb.Append("reader.").Append(readerMethod).Append("(columnOffset);");
                    }
                    sb.NewLine().Append("break;").NewLine().Outdent(false)
                        .Append("case ").Append(token + memberCount).Append(":").NewLine().Indent(false)
                        .Append("result.").Append(member.CodeName).Append(" = ").Append(nullCheck)
                        .Append("GetValue<")
                        .Append(Inspection.MakeNonNullable(memberType)).Append(">(reader, columnOffset);").NewLine()
                        .Append("break;").NewLine().Outdent(false);
                    token++;
                }
            }

            sb.Outdent().NewLine().Append("columnOffset++;").NewLine().Outdent().NewLine();
        }
        sb.Append("return result;").NewLine().Outdent().NewLine();


        sb.Outdent().NewLine().NewLine();




        static bool CouldBeNullable(ITypeSymbol symbol) => symbol.IsValueType
            ? symbol.NullableAnnotation == NullableAnnotation.Annotated
            : symbol.NullableAnnotation != NullableAnnotation.NotAnnotated;
    }

    [Flags]
    enum WriteArgsFlags
    {
        None = 0,
        NeedsTest = 1 << 0,
        NeedsPostProcess = 1 << 1,
        NeedsRowCount = 1 << 2,
        CanPrepare = 1 << 3,
    }

    enum WriteArgsMode
    {
        Add, Update, PostProcess,
        SetRowCount
    }

    private static void WriteArgs(ITypeSymbol? parameterType, CodeWriter sb, WriteArgsMode mode, string map, ref WriteArgsFlags flags)
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
        foreach (var member in Inspection.GetMembers(parameterType))
        {
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

            if (first)
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
                    sb.Append("p = cmd.CreateParameter();").NewLine()
                        .Append("p.ParameterName = ").AppendVerbatimLiteral(member.DbName).Append(";").NewLine();

                    var dbType = member.GetDbType(out _);
                    var size = member.TryGetValue<int>("Size");
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
                                    size = -1; // default to [n]varchar(max)/varbinary(max)
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
                    }).Append(";").NewLine().Append("p.Value = ");
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

    private ImmutableArray<ITypeSymbol> IdentifyDbCommandTypes(Compilation compilation, out bool needsPrepare)
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

    sealed class SourceState
    {
        private readonly object? diagnostics;

        public Location Location { get; }
        public OperationFlags Flags { get; }
        public string? Sql { get; }
        public string ParameterMap { get; }
        public IMethodSymbol Method { get; }
        public ITypeSymbol? ResultType { get; }
        public ITypeSymbol? ParameterType { get; }
        public SourceState(Location location, IMethodSymbol method, OperationFlags flags, string? sql,
            ITypeSymbol? resultType, ITypeSymbol? parameterType, string parameterMap, object? diagnostics = null)
        {
            Location = location;
            Flags = flags;
            Sql = sql;
            ResultType = resultType;
            ParameterType = parameterType;
            Method = method;
            ParameterMap = parameterMap;
            this.diagnostics = diagnostics;
        }

        public int DiagnosticCount => diagnostics switch
        {
            null => 0,
            Diagnostic => 1,
            IReadOnlyList<Diagnostic> list => list.Count,
            _ => -1
        };

        public Diagnostic GetDiagnostic(int index) => diagnostics switch
        {
            Diagnostic d when index is 0 => d,
            IReadOnlyList<Diagnostic> list => list[index],
            _ => throw new IndexOutOfRangeException(nameof(index)),
        };

        public (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation) Group()
            => new(Flags, Method, ParameterType, ParameterMap, (Flags & OperationFlags.CacheCommand) == 0 ? null : Location);
    }
    private sealed class CommonComparer : IEqualityComparer<(OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation)>, IComparer<Location>
    {
        public static readonly CommonComparer Instance = new();
        private CommonComparer() { }

        public int Compare(Location x, Location y)
        {
            if (x is null) { return y is null ? 0 : 1; }
            if (y is null) return -1;
            var xSpan = x.GetLineSpan();
            var ySpan = y.GetLineSpan();
            var delta = StringComparer.InvariantCulture.Compare(xSpan.Path, ySpan.Path);
            if (delta == 0)
            {
                delta = xSpan.StartLinePosition.CompareTo(ySpan.StartLinePosition);
            }
            if (delta == 0)
            {
                delta = xSpan.EndLinePosition.CompareTo(ySpan.EndLinePosition);
            }
            return delta;
        }

        public bool Equals((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation) x, (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation) y) => x.Flags == y.Flags
                && x.ParameterMap == y.ParameterMap
                && SymbolEqualityComparer.Default.Equals(x.Method, y.Method)
                && SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType)
                && x.UniqueLocation == y.UniqueLocation;

        public int GetHashCode((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap, Location? UniqueLocation) obj)
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
            return hash;
        }
    }
}
using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static Dapper.Internal.Inspection;

namespace Dapper.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed partial class DapperAnalyzer : DiagnosticAnalyzer
{
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticsBase.All<Diagnostics>();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1026:Enable concurrent execution", Justification = "Only disabled for debugging")]
    public override void Initialize(AnalysisContext context)
    {
#if !DEBUG
        context.EnableConcurrentExecution();
#endif
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // per-run state (in particular, so we can have "first time only" diagnostics)
        var state = new AnalyzerState(context);

        // respond to method usages
        context.RegisterOperationAction(state.OnOperation, OperationKind.Invocation, OperationKind.SimpleAssignment);

        // final actions
        context.RegisterCompilationEndAction(state.OnCompilationEndAction);
    }



    private sealed class AnalyzerState
    {
        private readonly List<Location> _missedOpportunities = new();
        private int _dapperHits; // this isn't a true count; it'll usually be 0 or 1, no matter the number of calls, because we stop tracking
        private void OnDapperAotHit()
        {
            if (Thread.VolatileRead(ref _dapperHits) == 0 && IsDapperAotAvailable) // fast short-circuit if we know we're all good
            {
                lock (_missedOpportunities)
                {
                    if (++_dapperHits == 1) // no point using Interlocked here; we need the lock anyway, to reset the list
                    {
                        // we only report about enabling Dapper.AOT if it isn't done anywhere
                        // (i.e. probably not known about), so: if they've *used* Dapper:
                        // we can stop tracking our failures
                        _missedOpportunities.Clear();
                    }
                }
            }
        }
        private void OnDapperAotMiss(OperationAnalysisContext ctx, Location location)
        {
            if (Thread.VolatileRead(ref _dapperHits) == 0 // fast short-circuit if we know we're all good
                && IsDapperAotAvailable) // don't warn if not available!
            {
                lock (_missedOpportunities)
                {
                    if (_dapperHits == 0)
                    {
                        // see comment in OnDapperAotHit
                        _missedOpportunities.Add(location);
                    }
                }
            }
        }

        internal void OnCompilationEndAction(CompilationAnalysisContext ctx)
        {
            lock (_missedOpportunities)
            {
                var count = _missedOpportunities.Count;
                if (count != 0)
                {
                    var location = _missedOpportunities[0];
                    Location[]? additionalLocations = null;
                    if (count > 1)
                    {
                        additionalLocations = new Location[count - 1];
                        _missedOpportunities.CopyTo(1, additionalLocations, 0, count - 1);
                    }
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.DapperAotNotEnabled, location, additionalLocations, count));
                }
            }
        }

        public void OnOperation(OperationAnalysisContext ctx)
        {
            try
            {
                // we'll look for:
                // method calls with a parameter called "sql" or marked [Sql]
                // property assignments to "CommandText"
                switch (ctx.Operation.Kind)
                {
                    case OperationKind.Invocation when ctx.Operation is IInvocationOperation invoke:
                        int index = 0;
                        foreach (var p in invoke.TargetMethod.Parameters)
                        {
                            if (p.Type.SpecialType == SpecialType.System_String && (
                                p.Name == "sql" || Inspection.GetDapperAttribute(p, Types.SqlAttribute) is not null))
                            {
                                if (Inspection.IsDapperMethod(invoke, out var dapperFlags))
                                {
                                    ValidateDapperMethod(ctx, invoke.Arguments[index], dapperFlags);
                                }
                                else
                                {
                                    ValidateParameterUsage(ctx, invoke.Arguments[index]);
                                }
                            }
                            index++;
                        }
                        break;
                    case OperationKind.SimpleAssignment when ctx.Operation is ISimpleAssignmentOperation assignment
                        && assignment.Target is IPropertyReferenceOperation propRef:
                        if (propRef.Member is IPropertySymbol
                            {
                                Type.SpecialType: SpecialType.System_String,
                                IsStatic: false,
                                IsIndexer: false,
                                // check for DbConnection?
                            } prop)
                        {
                            if (prop.Name == "CommandText" && Inspection.IsCommand(prop.ContainingType))
                            {
                                // write to CommandText
                                ValidatePropertyUsage(ctx, assignment.Value, true);
                            }
                            else if (Inspection.GetDapperAttribute(prop, Types.SqlAttribute) is not null)
                            {
                                // write SQL to a string property
                                ValidatePropertyUsage(ctx, assignment.Value, false);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnknownError, null, ex.Message, ex.StackTrace));
            }
        }

        private void ValidateDapperMethod(in OperationAnalysisContext ctx, IOperation sqlSource, OperationFlags flags)
        {
            Action<Diagnostic> onDiagnostic = ctx.ReportDiagnostic;
            if (ctx.Operation is not IInvocationOperation invoke) return;

            // check the args
            var parseState = new ParseState(ctx);
            bool aotEnabled = Inspection.IsEnabled(in parseState, invoke, Types.DapperAotAttribute, out var aotAttribExists);
            if (!aotEnabled) flags |= OperationFlags.DoNotGenerate;
            var location = SharedParseArgsAndFlags(parseState, invoke, ref flags, out var sql, out var paramType, onDiagnostic, out _);

            // report our AOT readiness
            if (aotEnabled)
            {
                OnDapperAotHit(); // all good for AOT
                if (flags.HasAny(OperationFlags.NotAotSupported))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedMethod, location, invoke.GetSignature()));
                }
            }
            else if (!aotAttribExists && !flags.HasAny(OperationFlags.NotAotSupported))
            {
                // we might have been able to do more, but Dapper.AOT wasn't enabled
                OnDapperAotMiss(ctx, location);
            }

            // check the types
            var resultMap = MemberMap.Create(invoke.GetResultType(flags), false);
            if (resultMap is not null)
            {
                // check for single-row value-type usage
                if (flags.HasAny(OperationFlags.SingleRow) && !flags.HasAny(OperationFlags.AtLeastOne)
                    && resultMap.ElementType.IsValueType && !Inspection.CouldBeNullable(resultMap.ElementType))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ValueTypeSingleFirstOrDefaultUsage, location, resultMap.ElementType.Name, invoke.TargetMethod.Name));
                }

                // check for constructors on the materialized type
                DiagnosticDescriptor? ctorFault = Inspection.ChooseConstructor(resultMap.ElementType, out var ctor) switch
                {
                    Inspection.ConstructorResult.FailMultipleExplicit => Diagnostics.ConstructorMultipleExplicit,
                    Inspection.ConstructorResult.FailMultipleImplicit when aotEnabled => Diagnostics.ConstructorAmbiguous,
                    _ => null,
                };
                if (ctorFault is not null)
                {
                    var loc = ctor?.Locations.FirstOrDefault() ?? resultMap.ElementType.Locations.FirstOrDefault();
                    ctx.ReportDiagnostic(Diagnostic.Create(ctorFault, loc, resultMap.ElementType.GetDisplayString()));
                }
            }

            var map = MemberMap.Create(paramType, true);
            var args = SharedGetParametersToInclude(map, flags, sql, onDiagnostic, out var parseFlags);


            ValidateSql(ctx, sqlSource, GetModeFlags(flags), location);

            /*
             * 

            if (paramNames.IsEmpty && (parseFlags & SqlParseOutputFlags.Return) == 0) // return is a parameter, sort of
            {
                if (flags.HasAny(OperationFlags.HasParameters))
                {
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.SqlParametersNotDetected, loc));
                }
                return "";
            }

            // so, we definitely detect parameters (note: don't warn just for return)
            if (!flags.HasAny(OperationFlags.HasParameters))
            {
                if (!paramNames.IsEmpty)
                {
                    Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.NoParametersSupplied, loc));
                }
                return "";
            }

                                // we can only consider this an error if we're confident in how well we parsed the input
                    // (unless we detected dynamic args, in which case: we don't know what we don't know)
                    if ((parseFlags & (SqlParseOutputFlags.Reliable | SqlParseOutputFlags.KnownParameters)) == (SqlParseOutputFlags.Reliable | SqlParseOutputFlags.KnownParameters))
                    {
                        Diagnostics.Add(ref diagnostics, Diagnostic.Create(Diagnostics.SqlParameterNotBound, loc, sqlParamName, CodeWriter.GetTypeName(parameterType)));
                    }
            */

            ValidateSurroundingLinqUsage(ctx, flags);

        }

        static SqlParseInputFlags GetModeFlags(OperationFlags flags)
        {

            var modeFlags = SqlParseInputFlags.None;
            // if (caseSensitive) modeFlags |= ModeFlags.CaseSensitive;
            if ((flags & OperationFlags.BindResultsByName) != 0) modeFlags |= SqlParseInputFlags.ValidateSelectNames;
            if ((flags & OperationFlags.SingleRow) != 0) modeFlags |= SqlParseInputFlags.SingleRow;
            if ((flags & OperationFlags.AtMostOne) != 0) modeFlags |= SqlParseInputFlags.AtMostOne;
            if ((flags & OperationFlags.Scalar) != 0)
            {
                modeFlags |= SqlParseInputFlags.ExpectQuery | SqlParseInputFlags.SingleRow | SqlParseInputFlags.SingleQuery | SqlParseInputFlags.SingleColumn;
            }
            else if ((flags & OperationFlags.Query) != 0)
            {
                modeFlags |= SqlParseInputFlags.ExpectQuery;
                if ((flags & OperationFlags.QueryMultiple) == 0) modeFlags |= SqlParseInputFlags.SingleQuery;
            }
            else if ((flags & (OperationFlags.Execute)) != 0) modeFlags |= SqlParseInputFlags.ExpectNoQuery;

            return modeFlags;
        }

        private void ValidateSurroundingLinqUsage(in OperationAnalysisContext ctx, OperationFlags flags)
        {
            if (flags.HasAny(OperationFlags.Query) && !flags.HasAny(OperationFlags.SingleRow)
                && ctx.Operation.Parent is IArgumentOperation arg
                && arg.Parent is IInvocationOperation parent && parent.TargetMethod is
                {
                    IsExtensionMethod: true,
                    Parameters.Length: 1, Arity: 1, ContainingType:
                    {
                        Name: nameof(Enumerable),
                        ContainingType: null,
                        ContainingNamespace:
                        {
                            Name: "Linq",
                            ContainingNamespace:
                            {
                                Name: "System",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    }
                } target)
            {
                bool useSingleRowQuery = parent.TargetMethod.Name switch
                {
                    nameof(Enumerable.First) => true,
                    nameof(Enumerable.Single) => true,
                    nameof(Enumerable.FirstOrDefault) => true,
                    nameof(Enumerable.SingleOrDefault) => true,
                    _ => false,
                };
                if (useSingleRowQuery)
                {
                    var name = parent.TargetMethod.Name;
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UseSingleRowQuery, parent.GetMemberLocation(),
                        "Query" + name, name));
                }
                else if (parent.TargetMethod.Name == nameof(Enumerable.ToList))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UseQueryAsList, parent.GetMemberLocation()));
                }
            }
        }

        private void ValidateParameterUsage(in OperationAnalysisContext ctx, IOperation sqlSource)
        {
            // TODO: check other parameters for special markers like command type?
            var flags = SqlParseInputFlags.None;
            ValidateSql(ctx, sqlSource, flags);
        }

        private void ValidatePropertyUsage(in OperationAnalysisContext ctx, IOperation sqlSource, bool isCommand)
        {
            var flags = SqlParseInputFlags.None;
            if (isCommand)
            {
                // TODO: check other parameters for special markers like command type?
                // check the method being invoked - scalar, reader, etc
            }
            ValidateSql(ctx, sqlSource, flags);
        }

        private static readonly Regex HasWhitespace = new Regex(@"\s", RegexOptions.Compiled | RegexOptions.Multiline);

        
        private readonly SqlSyntax? DefaultSqlSyntax;
        private readonly SqlParseInputFlags? DebugSqlFlags;
        private readonly Compilation _compilation;
        private bool? _isDapperAotAvailable;

        internal bool IsDapperAotAvailable => _isDapperAotAvailable ?? LazyIsDapperAotAvailable();
        private bool LazyIsDapperAotAvailable()
        {
            _isDapperAotAvailable = _compilation.Language == LanguageNames.CSharp &&  _compilation.GetTypeByMetadataName("Dapper." + Types.DapperAotAttribute) is not null;
            return _isDapperAotAvailable.Value;
        }
#pragma warning disable RS1012 // Start action has no registered actions
        public AnalyzerState(CompilationStartAnalysisContext context)
#pragma warning restore RS1012 // Start action has no registered actions
        {
            _compilation = context.Compilation;
            if (context.Options.TryGetSqlSyntax(out var syntax))
            {
                DefaultSqlSyntax = syntax;
            }
            if (context.Options.TryGetDebugModeFlags(out var flags))
            {
                DebugSqlFlags = flags;
            }
        }

        private void ValidateSql(in OperationAnalysisContext ctx, IOperation sqlSource, SqlParseInputFlags flags, Location? location = null)
        {
            var parseState = new ParseState(ctx);

            // should we consider this as a syntax we can handle?
            var syntax = Inspection.IdentifySqlSyntax(parseState, ctx.Operation, out var caseSensitive) ?? DefaultSqlSyntax ?? SqlSyntax.General;
            switch (syntax)
            {
                case SqlSyntax.SqlServer:
                    // other known types here
                    break;
                default:
                    return;
            }
            if (syntax != SqlSyntax.SqlServer) return;

            if (caseSensitive) flags |= SqlParseInputFlags.CaseSensitive;

            // can we get the SQL itself?
            if (!Inspection.TryGetConstantValueWithSyntax(sqlSource, out string? sql, out var sqlSyntax)) return;
            if (string.IsNullOrWhiteSpace(sql) || !HasWhitespace.IsMatch(sql)) return; // need non-trivial content to validate

            location ??= ctx.Operation.Syntax.GetLocation();

            if (DebugSqlFlags is not null)
            {
                var debugModeFlags = DebugSqlFlags.Value;
                if ((debugModeFlags & SqlParseInputFlags.DebugMode) != 0)
                {
                    // debug mode flags **replace** all other flags
                    flags = debugModeFlags;
                }
                else
                {
                    // otherwise we *extend* the flags
                    flags |= debugModeFlags;
                }
            }

            try
            {
                SqlParseOutputFlags parseFlags;
                switch (syntax)
                {
                    case SqlSyntax.SqlServer:

                        var proc = new OperationAnalysisContextTSqlProcessor(ctx, null, flags, location, sqlSyntax);
                        proc.Execute(sql!, members: default);
                        parseFlags = proc.Flags;
                        // paramMembers);
                        //parseFlags = proc.Flags;
                        //paramNames = (from var in proc.Variables
                        //              where var.IsParameter
                        //              select var.Name.StartsWith("@") ? var.Name.Substring(1) : var.Name
                        //              ).ToImmutableHashSet();
                        //diagnostics = proc.DiagnosticsObject;
                        break;
                    default:
                        parseFlags = SqlParseOutputFlags.None;
                        break;
                }

                // if we're sure we understood the SQL (reliable and no syntax error), we can check our query expectations
                // (note some other similar rules are enforced *inside* the parser - single-row, for example)
                if ((parseFlags & (SqlParseOutputFlags.SyntaxError | SqlParseOutputFlags.Reliable)) == SqlParseOutputFlags.Reliable)
                {
                    switch (flags & (SqlParseInputFlags.ExpectQuery | SqlParseInputFlags.ExpectNoQuery))
                    {
                        case SqlParseInputFlags.ExpectQuery when ((parseFlags & (SqlParseOutputFlags.Query | SqlParseOutputFlags.MaybeQuery)) == 0):
                            // definitely do not have a query
                            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.QueryCommandMissingQuery, location));
                            break;
                        case SqlParseInputFlags.ExpectNoQuery when ((parseFlags & SqlParseOutputFlags.Query) != 0):
                            // definitely have a query
                            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ExecuteCommandWithQuery, location));
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.GeneralSqlError, location, ex.Message));
            }
        }
    }

    // we want a common understanding of the setup between the analyzer and generator
    internal static Location SharedParseArgsAndFlags(in ParseState ctx, IInvocationOperation op, ref OperationFlags flags, out string? sql, out ITypeSymbol? paramType,
        Action<Diagnostic>? reportDiagnostic, out ITypeSymbol? resultType)
    {
        var callLocation = op.GetMemberLocation();
        Location? argLocation = null;
        sql = null;
        paramType = null;
        bool? buffered = null;
        // check the args
        foreach (var arg in op.Arguments)
        {

            switch (arg.Parameter?.Name)
            {
                case "sql":
                    if (Inspection.TryGetConstantValueWithSyntax(arg, out string? s, out _))
                    {
                        sql = s;
                    }
                    break;
                case "buffered":
                    if (Inspection.TryGetConstantValue(arg, out bool b))
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
                        if (expr.ConstantValue.HasValue && expr.ConstantValue.Value is null)
                        {
                            // another way of saying null, especially in VB
                        }
                        else
                        {
                            paramType = expr?.Type;
                            flags |= OperationFlags.HasParameters;
                        }
                    }
                    argLocation = arg.Syntax.GetLocation();
                    break;
                case "cnn":
                case "commandTimeout":
                case "transaction":
                    // nothing to do
                    break;
                case "commandType":
                    if (Inspection.TryGetConstantValue(arg, out int? ct))
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
                            default: // treat as flexible
                                reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.UnexpectedCommandType, arg.Syntax.GetLocation()));
                                break;
                        }
                    }
                    break;
                default:
                    if (!flags.HasAny(OperationFlags.NotAotSupported | OperationFlags.DoNotGenerate))
                    {
                        flags |= OperationFlags.DoNotGenerate;
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.UnexpectedArgument, arg.Syntax.GetLocation(), arg.Parameter?.Name));
                    }
                    break;
            }
        }
        // additional flags
        if (Inspection.IsEnabled(ctx, op, Types.CacheCommandAttribute, out _))
        {
            flags |= OperationFlags.CacheCommand;
        }

        if (!string.IsNullOrWhiteSpace(sql) && flags.HasAny(OperationFlags.Text)
            && Inspection.IsEnabled(ctx, op, Types.IncludeLocationAttribute, out _))
        {
            flags |= OperationFlags.IncludeLocation;
        }

        if (flags.HasAny(OperationFlags.Query) && buffered.HasValue)
        {
            flags |= buffered.GetValueOrDefault() ? OperationFlags.Buffered : OperationFlags.Unbuffered;
        }
        resultType = op.GetResultType(flags);
        if (flags.HasAny(OperationFlags.Query) && Inspection.IdentifyDbType(resultType, out _) is null)
        {
            flags |= OperationFlags.BindResultsByName;
        }

        if (flags.HasAny(OperationFlags.Query) || flags.HasAll(OperationFlags.Execute | OperationFlags.Scalar))
        {
            bool resultTuple = Inspection.InvolvesTupleType(resultType, out var withNames), bindByNameDefined = false;
            // tuples are positional by default
            if (resultTuple && !Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out bindByNameDefined))
            {
                flags &= ~OperationFlags.BindResultsByName;
            }

            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (resultTuple && withNames && Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out _))
                {   // Dapper vanilla supports bind-by-position for tuples; warn if bind-by-name is enabled
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperLegacyBindNameTupleResults, callLocation));
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (Inspection.InvolvesGenericTypeParameter(resultType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.GenericTypeParameter, callLocation, resultType!.ToDisplayString()));
                }
                else if (resultTuple)
                {
                    if (!bindByNameDefined)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperAotAddBindTupleByName, callLocation));
                    }

                    // but not implemented currently!
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperAotTupleResults, callLocation));
                }
                else if (!Inspection.IsPublicOrAssemblyLocal(resultType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.NonPublicType, callLocation, failing!.ToDisplayString(), Inspection.NameAccessibility(failing)));
                }
            }
        }

        // additional parameter checks
        if (flags.HasAny(OperationFlags.HasParameters))
        {
            argLocation ??= callLocation;
            bool paramTuple = Inspection.InvolvesTupleType(paramType, out _);
            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (paramTuple)
                {
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperLegacyTupleParameter, argLocation));
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (paramTuple)
                {
                    if (Inspection.IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out var defined))
                    {
                        flags |= OperationFlags.BindTupleParameterByName;
                    }
                    if (!defined)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperAotAddBindTupleByName, argLocation));
                    }

                    // but not implemented currently!
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperAotTupleParameter, argLocation));
                }
                else if (Inspection.InvolvesGenericTypeParameter(paramType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.GenericTypeParameter, argLocation, paramType!.ToDisplayString()));
                }
                else if (Inspection.IsMissingOrObjectOrDynamic(paramType) || Inspection.IsDynamicParameters(paramType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.UntypedParameter, argLocation));
                }
                else if (!Inspection.IsPublicOrAssemblyLocal(paramType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.NonPublicType, argLocation, failing!.ToDisplayString(), Inspection.NameAccessibility(failing)));
                }
            }
        }
        return callLocation;
    }

    enum ParameterMode
    {
        None, All, Defer, Filter
    }

    internal static AdditionalCommandState? SharedGetAdditionalCommandState(ISymbol target, MemberMap? map, Action<Diagnostic>? reportDiagnostic)
    {
        var attribs = target.GetAttributes();
        if (attribs.IsDefaultOrEmpty) return null;

        int cmdPropsCount = 0;
        int estimatedRowCount = 0;
        string? estimatedRowCountMember = null;
        if (map is not null)
        {
            foreach (var member in map.Members)
            {
                if (member.IsEstimatedRowCount)
                {
                    if (estimatedRowCountMember is not null)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MemberRowCountHintDuplicated, member.GetLocation()));
                    }
                    estimatedRowCountMember = member.Member.Name;
                }
            }
        }
        var location = target.Locations.FirstOrDefault();
        foreach (var attrib in attribs)
        {
            if (Inspection.IsDapperAttribute(attrib))
            {
                switch (attrib.AttributeClass!.Name)
                {
                    case Types.EstimatedRowCountAttribute:
                        if (attrib.ConstructorArguments.Length == 1 && attrib.ConstructorArguments[0].Value is int i
                            && i > 0)
                        {
                            if (estimatedRowCountMember is null)
                            {
                                estimatedRowCount = i;
                            }
                            else
                            {
                                reportDiagnostic?.Invoke(Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MethodRowCountHintRedundant, location, estimatedRowCountMember));
                            }
                        }
                        else
                        {
                            reportDiagnostic?.Invoke(Diagnostic.Create(DapperInterceptorGenerator.Diagnostics.MethodRowCountHintInvalid, location));
                        }
                        break;
                    case Types.CommandPropertyAttribute:
                        cmdPropsCount++;
                        break;
                }
            }
        }

        ImmutableArray<CommandProperty> cmdProps;
        if (cmdPropsCount != 0)
        {
            var builder = ImmutableArray.CreateBuilder<CommandProperty>(cmdPropsCount);
            foreach (var attrib in attribs)
            {
                if (Inspection.IsDapperAttribute(attrib) && attrib.AttributeClass!.Name == Types.CommandPropertyAttribute
                    && attrib.AttributeClass.Arity == 1
                    && attrib.AttributeClass.TypeArguments[0] is INamedTypeSymbol cmdType
                    && attrib.ConstructorArguments.Length == 2
                    && attrib.ConstructorArguments[0].Value is string name
                    && attrib.ConstructorArguments[1].Value is object value)
                {
                    builder.Add(new(cmdType, name, value, location));
                }
            }
            cmdProps = builder.ToImmutable();
        }
        else
        {
            cmdProps = ImmutableArray<CommandProperty>.Empty;
        }


        return cmdProps.IsDefaultOrEmpty && estimatedRowCount <= 0 && estimatedRowCountMember is null
            ? null : new(estimatedRowCount, estimatedRowCountMember, cmdProps);
    }

    internal static ImmutableArray<ElementMember>? SharedGetParametersToInclude(MemberMap? map, OperationFlags flags, string? sql, Action<Diagnostic>? reportDiagnostic, out SqlParseOutputFlags parseFlags)
    {
        SortedDictionary<string, ElementMember>? byDbName = null;
        var filter = ImmutableHashSet<string>.Empty;
        ParameterMode mode;
        if (flags.HasAny(OperationFlags.StoredProcedure | OperationFlags.TableDirect))
        {
            parseFlags = flags.HasAny(OperationFlags.StoredProcedure) ? SqlParseOutputFlags.MaybeQuery : SqlParseOutputFlags.Query;
            mode = ParameterMode.All;
        }
        else
        {
            parseFlags = SqlParseOutputFlags.MaybeQuery;

            // if command-type or command is not known statically: defer decision
            if (!flags.HasAny(OperationFlags.Text) || string.IsNullOrWhiteSpace(sql))
            {
                mode = ParameterMode.Defer;
            }
            else
            {
                mode = ParameterMode.Filter;
                filter = SqlTools.GetUniqueParameters(sql);
            }
        }
        if (!flags.HasAny(OperationFlags.HasParameters))
        {
            mode = ParameterMode.None;
        }

        if (!(map is null || map.IsUnknownParameters))
        {
            // check the shape of the parameter type
            string? rowCountMember = null, returnCodeMember = null;
            foreach (var member in map.Members)
            {
                if (member.IsRowCount)
                {
                    if (rowCountMember is not null)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateRowCount, member.GetLocation(), member.CodeName, rowCountMember));
                    }
                    else
                    {
                        rowCountMember = member.CodeName;
                    }
                    if (member.HasDbValueAttribute)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.RowCountDbValue, member.GetLocation(), member.CodeName));
                    }
                }
                if (member.Kind != ElementMemberKind.None)
                {
                    continue; // not treated as parameters for naming etc purposes
                }

                var dbName = member.DbName;
                bool isRetVal = member.Direction == ParameterDirection.ReturnValue && flags.HasAny(OperationFlags.StoredProcedure);
                switch (mode)
                {
                    case ParameterMode.None:
                    case ParameterMode.Defer:
                        continue; // don't include anything
                    case ParameterMode.Filter:
                        if (isRetVal)
                        {   // always include retval on sproc
                            break;
                        }
                        if (!filter.Contains(dbName))
                        {
                            // exclude other things that we can't see
                            continue;
                        }
                        break;
                    case ParameterMode.All:
                        break; // g
                }

                byDbName ??= new(StringComparer.InvariantCultureIgnoreCase);
                if (byDbName.TryGetValue(dbName, out var existing))
                {
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateParameter, member.GetLocation(), member, existing, dbName));
                }
                else
                {
                    byDbName.Add(dbName, member);
                }

                if (isRetVal)
                {
                    if (returnCodeMember is null)
                    {
                        returnCodeMember = member.CodeName;
                    }
                    else
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateReturn, member.GetLocation(), member.CodeName, returnCodeMember));
                    }
                }
            }
        }

        if (mode == ParameterMode.Defer)
        {
            return null;
        }
        if (byDbName is null)
        {
            // nothing found
            return ImmutableArray<ElementMember>.Empty;
        }
        if (byDbName.Count == 0)
        {
            // so common that it is worth special-casing
            return ImmutableArray<ElementMember>.Empty.Add(byDbName.Single().Value);
        }
        var builder = ImmutableArray.CreateBuilder<ElementMember>(byDbName.Count);
        // add everything we found, keeping "result" at the end; note already sorted
        foreach (var pair in byDbName)
        {
            var member = pair.Value;
            if (member.Direction != ParameterDirection.ReturnValue)
            {
                builder.Add(member);
            }
        }
        foreach (var pair in byDbName)
        {
            var member = pair.Value;
            if (member.Direction == ParameterDirection.ReturnValue)
            {
                builder.Add(member);
            }
        }
        return builder.ToImmutable();

    }
}
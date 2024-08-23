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
        private readonly List<Location> _missedOpportunities = [];
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
        private void OnDapperAotMiss(Location location)
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
            try
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
            catch (Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticsBase.UnknownError, null, ex.Message, ex.StackTrace));
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
                                p.Name == "sql" || GetDapperAttribute(p, Types.SqlAttribute) is not null))
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
                            if (prop.Name == "CommandText" && IsCommand(prop.ContainingType))
                            {
                                // write to CommandText
                                ValidatePropertyUsage(ctx, assignment.Value, true);
                            }
                            else if (GetDapperAttribute(prop, Types.SqlAttribute) is not null)
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
                ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticsBase.UnknownError, null, ex.Message, ex.StackTrace));
            }
        }

        private void ValidateDapperMethod(in OperationAnalysisContext ctx, IOperation sqlSource, OperationFlags flags)
        {
            Action<Diagnostic> onDiagnostic = ctx.ReportDiagnostic;
            if (ctx.Operation is not IInvocationOperation invoke) return;

            // check the args
            var parseState = new ParseState(ctx);
            bool aotEnabled = IsEnabled(in parseState, invoke, Types.DapperAotAttribute, out var aotAttribExists);
            if (!aotEnabled) flags |= OperationFlags.DoNotGenerate;
            var location = SharedParseArgsAndFlags(parseState, invoke, ref flags, out var sql, out var argExpression, onDiagnostic, out _, exitFirstFailure: false);

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
                OnDapperAotMiss(location);
            }

            // check the types
            // resultType can be an array, so let's unwrap it to determine actual type
            var resultType = invoke.GetResultType(flags);
            if (resultType is not null && IdentifyDbType(resultType, out _) is null && !IsSpecialCaseAllowedResultType(resultType)) // don't warn if handled as an inbuilt
            {
                var resultMap = MemberMap.CreateForResults(resultType, location);
                if (resultMap is not null)
                {
                    ValidateMembers(resultMap, onDiagnostic);

                    // check for single-row value-type usage
                    if (flags.HasAny(OperationFlags.SingleRow) && !flags.HasAny(OperationFlags.AtLeastOne)
                        && resultMap.ElementType.IsValueType && !CouldBeNullable(resultMap.ElementType))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ValueTypeSingleFirstOrDefaultUsage, location, resultMap.ElementType.Name, invoke.TargetMethod.Name));
                    }

                    // check for constructors and factoryMethods on the materialized type
                    var ctorFault = ChooseConstructor(resultMap.ElementType, out var ctor) switch
                    {
                        ConstructorResult.FailMultipleExplicit => Diagnostics.ConstructorMultipleExplicit,
                        ConstructorResult.FailMultipleImplicit when aotEnabled => Diagnostics.ConstructorAmbiguous,
                        _ => null,
                    };
                    var factoryMethodFault = ChooseFactoryMethod(resultMap.ElementType, out var factoryMethod) switch
                    {
                        FactoryMethodResult.FailMultipleExplicit => Diagnostics.FactoryMethodMultipleExplicit,
                        _ => null,
                    };

                    // we cant use both ctor and factoryMethod, so reporting a warning that ctor is prioritized
                    if (ctor is not null && factoryMethod is not null)
                    {
                        var loc = factoryMethod?.Locations.FirstOrDefault() ?? resultMap.ElementType.Locations.FirstOrDefault();
                        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ConstructorOverridesFactoryMethod, loc, resultMap.ElementType.GetDisplayString()));
                    }

                    if (ctorFault is not null)
                    {
                        var loc = ctor?.Locations.FirstOrDefault() ?? resultMap.ElementType.Locations.FirstOrDefault();
                        ctx.ReportDiagnostic(Diagnostic.Create(ctorFault, loc, resultMap.ElementType.GetDisplayString()));
                    }
                    else if (factoryMethodFault is not null)
                    {
                        var loc = factoryMethod?.Locations.FirstOrDefault() ?? resultMap.ElementType.Locations.FirstOrDefault();
                        ctx.ReportDiagnostic(Diagnostic.Create(factoryMethodFault, loc, resultMap.ElementType.GetDisplayString()));
                    }
                    else if (resultMap.Members.IsDefaultOrEmpty && IsPublicOrAssemblyLocal(resultType, parseState, out _))
                    {
                        // there are so settable members + there is no constructor to use
                        // (note: generator uses Inspection.GetMembers(type, dapperAotConstructor: constructor)
                        ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.UserTypeNoSettableMembersFound, resultMap.Location, resultType.Name));
                    }
                }
            }

            var parameters = MemberMap.CreateForParameters(argExpression);
            if (aotEnabled)
            {
                _ = AdditionalCommandState.Parse(GetSymbol(parseState, invoke), parameters, onDiagnostic);
            }
            
            ValidateParameters(parameters, flags, onDiagnostic);

            var args = SharedGetParametersToInclude(parameters, ref flags, sql, onDiagnostic, out var parseFlags);

            ValidateSql(ctx, sqlSource, GetModeFlags(flags), SqlParameters.From(args), location);

            ValidateSurroundingLinqUsage(ctx, flags);

        }

        static SqlParseInputFlags GetModeFlags(OperationFlags flags)
        {
            var modeFlags = SqlParseInputFlags.None;
            // if (caseSensitive) modeFlags |= ModeFlags.CaseSensitive;
            if ((flags & OperationFlags.BindResultsByName) != 0) modeFlags |= SqlParseInputFlags.ValidateSelectNames;
            if ((flags & OperationFlags.SingleRow) != 0) modeFlags |= SqlParseInputFlags.SingleRow;
            if ((flags & OperationFlags.AtMostOne) != 0) modeFlags |= SqlParseInputFlags.AtMostOne;
            if ((flags & OperationFlags.KnownParameters) != 0) modeFlags |= SqlParseInputFlags.KnownParameters;
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
            ValidateSql(ctx, sqlSource, flags, SqlParameters.None);
        }

        private void ValidatePropertyUsage(in OperationAnalysisContext ctx, IOperation sqlSource, bool isCommand)
        {
            var flags = SqlParseInputFlags.None;
            if (isCommand)
            {
                // TODO: check other parameters for special markers like command type?
                // check the method being invoked - scalar, reader, etc
            }
            ValidateSql(ctx, sqlSource, flags, SqlParameters.None);
        }

        private readonly SqlSyntax? DefaultSqlSyntax;
        private readonly SqlParseInputFlags? DebugSqlFlags;
        private readonly Compilation _compilation;
        private bool? _isDapperAotAvailable;

        internal bool IsDapperAotAvailable => _isDapperAotAvailable ?? LazyIsDapperAotAvailable();
        private bool LazyIsDapperAotAvailable()
        {
            _isDapperAotAvailable = _compilation.Language == LanguageNames.CSharp && _compilation.GetTypeByMetadataName("Dapper." + Types.DapperAotAttribute) is not null;
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

        private void ValidateSql(in OperationAnalysisContext ctx, IOperation sqlSource, SqlParseInputFlags flags,
            ImmutableArray<SqlParameter> parameters, Location? location = null)
        {
            var parseState = new ParseState(ctx);

            // should we consider this as a syntax we can handle?
            var syntax = IdentifySqlSyntax(parseState, ctx.Operation, out var caseSensitive) ?? DefaultSqlSyntax ?? SqlSyntax.General;
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
            if (!TryGetConstantValueWithSyntax(sqlSource, out string? sql, out var sqlSyntax, out var stringSyntaxKind))
            {
                DiagnosticDescriptor? descriptor = stringSyntaxKind switch
                {
                    StringSyntaxKind.InterpolatedString
                        => Diagnostics.InterpolatedStringSqlExpression,
                    StringSyntaxKind.ConcatenatedString or StringSyntaxKind.FormatString
                        => Diagnostics.ConcatenatedStringSqlExpression,
                    _ => null
                };

                if (descriptor is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(descriptor, sqlSource.Syntax.GetLocation()));
                }
                return;
            }
            if (string.IsNullOrWhiteSpace(sql) || !CompiledRegex.WhitespaceOrReserved.IsMatch(sql)) return; // need non-trivial content to validate

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

            bool forgiveSyntaxErrors = false;
            try
            {
                SqlParseOutputFlags parseFlags;
                switch (syntax)
                {
                    case SqlSyntax.GeneralWithAtParameters:
                    case SqlSyntax.SQLite:
                    case SqlSyntax.MySql:
                        forgiveSyntaxErrors = true; // we're just taking a punt, honestly
                        goto case SqlSyntax.SqlServer;
                    case SqlSyntax.SqlServer:
                        var proc = new OperationAnalysisContextTSqlProcessor(ctx, null, flags, location, sqlSyntax);
                        proc.Execute(sql!, parameters);
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
                if (!forgiveSyntaxErrors)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.GeneralSqlError, location, ex.Message));
                }
            }
        }
    }

    // we want a common understanding of the setup between the analyzer and generator
    internal static Location SharedParseArgsAndFlags(in ParseState ctx, IInvocationOperation op, ref OperationFlags flags, out string? sql,
        out IOperation? argExpression, Action<Diagnostic>? reportDiagnostic, out ITypeSymbol? resultType, bool exitFirstFailure)
    {
        var callLocation = op.GetMemberLocation();
        argExpression = null;
        sql = null;
        bool? buffered = null;

        // check the args
        foreach (var arg in op.Arguments)
        {

            switch (arg.Parameter?.Name)
            {
                case "sql":
                    if (TryGetConstantValueWithSyntax(arg, out string? s, out _, out _))
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
                        while (expr is IConversionOperation conv && expr.Type?.SpecialType == SpecialType.System_Object)
                        {
                            expr = conv.Operand;
                        }
                        if (expr.ConstantValue.HasValue && expr.ConstantValue.Value is null)
                        {
                            // another way of saying null, especially in VB
                        }
                        else
                        {
                            argExpression = expr;
                            flags |= OperationFlags.HasParameters;
                        }
                    }
                    break;
                case "cnn":
                case "commandTimeout":
                case "transaction":
                case "reader":
                case "startIndex":
                case "length":
                case "returnNullIfFirstMissing":
                case "concreteType" when arg.Value is IDefaultValueOperation || (arg.ConstantValue.HasValue && arg.ConstantValue.Value is null):
                    // nothing to do
                    break;
                case "commandType":
                    if (TryGetConstantValue(arg, out int? ct))
                    {
                        switch (ct)
                        {
                            case null when !string.IsNullOrWhiteSpace(sql):
                                // if no spaces: interpret as stored proc, else: text
                                flags |= CompiledRegex.WhitespaceOrReserved.IsMatch(sql) ? OperationFlags.Text : OperationFlags.StoredProcedure;
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

        if (exitFirstFailure && flags.HasAny(OperationFlags.DoNotGenerate))
        {
            resultType = null;
            return callLocation;
        }

        // additional flags
        if (IsEnabled(ctx, op, Types.CacheCommandAttribute, out _))
        {
            flags |= OperationFlags.CacheCommand;
        }

        if (!string.IsNullOrWhiteSpace(sql) && flags.HasAny(OperationFlags.Text)
            && IsEnabled(ctx, op, Types.IncludeLocationAttribute, out _))
        {
            flags |= OperationFlags.IncludeLocation;
        }

        if (flags.HasAny(OperationFlags.Query) && buffered.HasValue)
        {
            flags |= buffered.GetValueOrDefault() ? OperationFlags.Buffered : OperationFlags.Unbuffered;
        }
        resultType = op.GetResultType(flags);
        if (flags.HasAny(OperationFlags.Query) && IdentifyDbType(resultType, out _) is null)
        {
            flags |= OperationFlags.BindResultsByName;
        }

        if (flags.HasAny(OperationFlags.Query) || flags.HasAll(OperationFlags.Execute | OperationFlags.Scalar))
        {
            bool resultTuple = Inspection.InvolvesTupleType(resultType, out var withNames), bindByNameDefined = false;
            // tuples are positional by default
            if (resultTuple && !IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out bindByNameDefined))
            {
                flags &= ~OperationFlags.BindResultsByName;
            }

            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (resultTuple && withNames && IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out _))
                {   // Dapper vanilla supports bind-by-position for tuples; warn if bind-by-name is enabled
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DapperLegacyBindNameTupleResults, callLocation));
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (InvolvesGenericTypeParameter(resultType))
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
                else if (!IsPublicOrAssemblyLocal(resultType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.NonPublicType, callLocation, failing!.ToDisplayString(), NameAccessibility(failing)));
                }
            }
        }

        if (exitFirstFailure && flags.HasAny(OperationFlags.DoNotGenerate))
        {
            resultType = null;
            return callLocation;
        }

        // additional parameter checks
        if (flags.HasAny(OperationFlags.HasParameters))
        {
            var argLocation = argExpression?.Syntax.GetLocation() ?? callLocation;
            var paramType = argExpression?.Type;
            bool paramTuple = Inspection.InvolvesTupleType(paramType, out _);
            if (flags.HasAny(OperationFlags.DoNotGenerate))
            {
                // extra checks specific to Dapper vanilla
                if (reportDiagnostic is not null)
                {
                    if (paramTuple)
                    {
                        reportDiagnostic.Invoke(Diagnostic.Create(Diagnostics.DapperLegacyTupleParameter, argLocation));
                    }
                }
            }
            else
            {
                // extra checks specific to DapperAOT
                if (paramTuple)
                {
                    if (IsEnabled(ctx, op, Types.BindTupleByNameAttribute, out var defined))
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
                else if (InvolvesGenericTypeParameter(paramType))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.GenericTypeParameter, argLocation, paramType!.ToDisplayString()));
                }
                else if (IsMissingOrObjectOrDynamic(paramType) || IsDynamicParameters(paramType, out _))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.UntypedParameter, argLocation));
                }
                else if (!IsPublicOrAssemblyLocal(paramType, ctx, out var failing))
                {
                    flags |= OperationFlags.DoNotGenerate;
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.NonPublicType, argLocation, failing!.ToDisplayString(), NameAccessibility(failing)));
                }
            }
        }
        return callLocation;
    }

    enum ParameterMode
    {
        None, All, Defer, Filter
    }

    internal static AdditionalCommandState? SharedGetAdditionalCommandState(ISymbol target, MemberMap? parameters, Action<Diagnostic>? reportDiagnostic)
    {
        var methodAttribs = target.GetAttributes();
        if (methodAttribs.IsDefaultOrEmpty && parameters is not { Members.IsDefaultOrEmpty: false }) return null;

        int cmdPropsCount = 0;
        int rowCountHint = 0;
        ElementMember? rowCountHintMember = null;
        var location = target.Locations.FirstOrDefault();

        if (parameters is not null)
        {
            foreach (var member in parameters.Members)
            {
                if (member.IsRowCountHint)
                {
                    if (rowCountHintMember is not null)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.RowCountHintDuplicated, member.GetLocation(Types.RowCountHintAttribute),
                            additionalLocations: rowCountHintMember.GetValueOrDefault().AsAdditionalLocations(Types.RowCountHintAttribute),
                            messageArgs: [member.CodeName]));
                    }
                    rowCountHintMember = member;

                    if (reportDiagnostic is not null)
                    {
                        foreach (var attrib in member.Member.GetAttributes())
                        {
                            switch (attrib.AttributeClass!.Name)
                            {
                                case Types.RowCountHintAttribute:
                                    if (attrib.ConstructorArguments.Length != 0)
                                    {
                                        reportDiagnostic.Invoke(Diagnostic.Create(Diagnostics.RowCountHintShouldNotSpecifyValue,
                                            attrib.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location));
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        int? batchSize = null;
        foreach (var attrib in methodAttribs)
        {
            if (IsDapperAttribute(attrib))
            {
                switch (attrib.AttributeClass!.Name)
                {
                    case Types.RowCountHintAttribute:
                        if (attrib.ConstructorArguments.Length == 1 && attrib.ConstructorArguments[0].Value is int i
                            && i > 0)
                        {
                            if (rowCountHintMember is null)
                            {
                                rowCountHint = i;
                            }
                            else
                            {
                                reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.RowCountHintRedundant,
                                    attrib.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location,
                                    rowCountHintMember.GetValueOrDefault().CodeName));
                            }
                        }
                        else
                        {
                            reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.RowCountHintInvalidValue,
                                attrib.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location,
                                location));
                        }
                        break;
                    case Types.CommandPropertyAttribute:
                        cmdPropsCount++;
                        break;
                    case Types.BatchSizeAttribute:
                        if (attrib.ConstructorArguments.Length == 1 && attrib.ConstructorArguments[0].Value is int batchTmp)
                        {
                            batchSize = batchTmp;
                        }
                        break;
                }
            }
        }

        ImmutableArray<CommandProperty> cmdProps;
        if (cmdPropsCount != 0)
        {
            var builder = ImmutableArray.CreateBuilder<CommandProperty>(cmdPropsCount);
            foreach (var attrib in methodAttribs)
            {
                if (IsDapperAttribute(attrib) && attrib.AttributeClass!.Name == Types.CommandPropertyAttribute
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


        return cmdProps.IsDefaultOrEmpty && rowCountHint <= 0 && rowCountHintMember is null && batchSize is null
            ? null : new(rowCountHint, rowCountHintMember?.Member.Name, batchSize, cmdProps);
    }

    static void ValidateParameters(MemberMap? parameters, OperationFlags flags, Action<Diagnostic> onDiagnostic)
    {
        if (parameters is null) return;

        var usingVanillaDapperMode = flags.HasAny(OperationFlags.DoNotGenerate); // using vanilla Dapper mode
        if (usingVanillaDapperMode)
        {
            if (parameters.Members.Any(s => s.IsCancellation) || IsCancellationToken(parameters.ElementType))
            {
                onDiagnostic(Diagnostic.Create(Diagnostics.CancellationNotSupported, parameters.Location));
            }
        }

        var isFirstCancellation = true;
        foreach (var member in parameters.Members)
        {
            ValidateCancellationTokenParameter(member);
            ValidateDbStringParameter(member);
        }

        void ValidateDbStringParameter(ElementMember member)
        {
            if (usingVanillaDapperMode)
            {
                // reporting ONLY in Dapper AOT
                return;
            }
            
            if (member.DapperSpecialType == DapperSpecialType.DbString)
            {
                onDiagnostic(Diagnostic.Create(Diagnostics.MoveFromDbString, member.GetLocation()));
            }
        }

        void ValidateCancellationTokenParameter(ElementMember member)
        {
            if (!usingVanillaDapperMode && member.IsCancellation)
            {
                if (isFirstCancellation) isFirstCancellation = false;
                else onDiagnostic(Diagnostic.Create(Diagnostics.CancellationDuplicated, member.GetLocation()));
            }
        }
    }

    static void ValidateMembers(MemberMap memberMap, Action<Diagnostic> onDiagnostic)
    {
        if (memberMap.Members.Length == 0)
        {
            return;
        }
        
        var normalizedPropertyNames = new Dictionary<string, List<Location>>();
        var normalizedFieldNames = new Dictionary<string, List<Location>>();

        foreach (var member in memberMap!.Members)
        {
            ValidateMember(member, onDiagnostic);

            // build collection of duplicate normalized memberNames with memberLocations
            Location? memberLoc;
            if ((memberLoc = member.GetLocation()) is not null)
            {
                var normalizedName = StringHashing.Normalize(member.CodeName);

                switch (member.SymbolKind)
                {
                    case SymbolKind.Property:
                        if (!normalizedPropertyNames.ContainsKey(normalizedName)) normalizedPropertyNames[normalizedName] = new();
                        normalizedPropertyNames[normalizedName].Add(memberLoc);
                        break;

                    case SymbolKind.Field:
                        if (!normalizedFieldNames.ContainsKey(normalizedName)) normalizedFieldNames[normalizedName] = new();
                        normalizedFieldNames[normalizedName].Add(memberLoc);
                        break;
                }
            }
        }

        ReportNormalizedMembersNamesCollisions(normalizedFieldNames, Diagnostics.AmbiguousFields);
        ReportNormalizedMembersNamesCollisions(normalizedPropertyNames, Diagnostics.AmbiguousProperties);

        void ReportNormalizedMembersNamesCollisions(Dictionary<string, List<Location>> nameMemberLocations, DiagnosticDescriptor diagnosticDescriptor)
        {
            if (nameMemberLocations.Count == 0) return;

            foreach (var entry in nameMemberLocations)
            {
                if (entry.Value?.Count <= 1)
                {
                    continue;
                }

                onDiagnostic?.Invoke(Diagnostic.Create(diagnosticDescriptor,
                        location: entry.Value.First(),
                        additionalLocations: entry.Value.Skip(1),
                        messageArgs: entry.Key));
            }
        }
    }

    static void ValidateMember(ElementMember member, Action<Diagnostic>? reportDiagnostic)
    {
        ValidateColumnAttribute();

        void ValidateColumnAttribute()
        {
            if (member.HasDbValueAttribute && member.ColumnAttributeData.IsCorrectUsage)
            {
                reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.ParameterNameOverrideConflict,
                    location: member.GetLocation(Types.DbValueAttribute),
                    additionalLocations: member.AsAdditionalLocations(Types.ColumnAttribute, allowNonDapperLocations: true),
                    messageArgs: member.DbName));
            }
            if (member.ColumnAttributeData.ColumnAttribute == ColumnAttributeData.ColumnAttributeState.Specified
                && member.ColumnAttributeData.UseColumnAttribute == ColumnAttributeData.UseColumnAttributeState.NotSpecified)
            {
                reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.UseColumnAttributeNotSpecified,
                    location: member.GetLocation(Types.ColumnAttribute, allowNonDapperLocations: true)));
            }
        }
    }

    internal static ImmutableArray<ElementMember>? SharedGetParametersToInclude(MemberMap? map, ref OperationFlags flags, string? sql, Action<Diagnostic>? reportDiagnostic, out SqlParseOutputFlags parseFlags)
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
            flags |= OperationFlags.KnownParameters;
            ElementMember? rowCountMember = null, returnCodeMember = null;
            foreach (var member in map.Members)
            {
                if (member.IsRowCount)
                {
                    if (rowCountMember is not null)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateRowCount,
                            location: member.GetLocation(Types.RowCountAttribute),
                            additionalLocations: rowCountMember.GetValueOrDefault().AsAdditionalLocations(Types.RowCountAttribute),
                            messageArgs: [rowCountMember.GetValueOrDefault().CodeName, member.CodeName]));
                    }
                    else
                    {
                        rowCountMember = member;
                    }
                    if (member.HasDbValueAttribute)
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.RowCountDbValue,
                            location: member.GetLocation(Types.DbValueAttribute),
                            additionalLocations: member.AsAdditionalLocations(Types.RowCountAttribute),
                            messageArgs: [member.CodeName]));
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
                    reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateParameter,
                        location: member.GetLocation(Types.DbValueAttribute),
                        additionalLocations: existing.AsAdditionalLocations(Types.DbValueAttribute),
                        messageArgs: [existing.CodeName, member.CodeName, dbName]));
                }
                else
                {
                    byDbName.Add(dbName, member);
                }

                if (isRetVal)
                {
                    if (returnCodeMember is null)
                    {
                        returnCodeMember = member;
                    }
                    else
                    {
                        reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.DuplicateReturn,
                            location: member.GetLocation(Types.DbValueAttribute),
                            additionalLocations: returnCodeMember.GetValueOrDefault().AsAdditionalLocations(Types.DbValueAttribute),
                            messageArgs: [returnCodeMember.GetValueOrDefault().CodeName, member.CodeName]));
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
            if (flags.HasAll(OperationFlags.HasParameters | OperationFlags.Text))
            {
                // has args, and is command-text
                reportDiagnostic?.Invoke(Diagnostic.Create(Diagnostics.SqlParametersNotDetected, map?.Location));
            }
            return ImmutableArray<ElementMember>.Empty;
        }
        if (byDbName.Count == 1)
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
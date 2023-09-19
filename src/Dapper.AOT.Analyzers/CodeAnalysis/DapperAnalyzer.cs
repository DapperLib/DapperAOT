using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Dapper.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed partial class DapperAnalyzer : DiagnosticAnalyzer
{
    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticsBase.All<Diagnostics>();

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // per-run state (in particular, so we can have "first time only" diagnostics)
        var state = new AnalyzerState();

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
            if (Thread.VolatileRead(ref _dapperHits) == 0) // fast short-circuit if we know we're all good
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
                && ctx.Compilation.Language == LanguageNames.CSharp) // no point warning for VB etc
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
                if (count != 0 && IsDapperAotAvailable(ctx.Compilation))
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

            static bool IsDapperAotAvailable(Compilation compilation)
                => compilation.GetTypeByMetadataName("Dapper." + Types.DapperAotAttribute) is not null;
        }
        //private int _missedOpportunities;
        //private bool IsFirstMiss() => Interlocked.Increment(ref _missedOpportunities) == 1;

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
            if (ctx.Operation is not IInvocationOperation invoke) return;

            var location = invoke.GetMemberLocation();
            var parseState = new ParseState(ctx);

            if (Inspection.IsEnabled(in parseState, invoke, Types.DapperAotAttribute, out var aotAttribExists))
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
                    Inspection.ConstructorResult.FailMultipleImplicit => Diagnostics.ConstructorAmbiguous,
                    _ => null,
                };
                if (ctorFault is not null)
                {
                    var loc = ctor?.Locations.FirstOrDefault() ?? resultMap.ElementType.Locations.FirstOrDefault();
                    ctx.ReportDiagnostic(Diagnostic.Create(ctorFault, loc, resultMap.ElementType.GetDisplayString()));
                }
            }

            ValidateSql(ctx, sqlSource, GetModeFlags(flags), location);

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

        private static void ValidateParameterUsage(in OperationAnalysisContext ctx, IOperation sqlSource)
        {
            // TODO: check other parameters for special markers like command type?
            var flags = SqlParseInputFlags.None;
            ValidateSql(ctx, sqlSource, flags);
        }

        private static void ValidatePropertyUsage(in OperationAnalysisContext ctx, IOperation sqlSource, bool isCommand)
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

        private static void ValidateSql(in OperationAnalysisContext ctx, IOperation sqlSource, SqlParseInputFlags flags, Location? location = null)
        {
            var parseState = new ParseState(ctx);

            // should we consider this as a syntax we can handle?
            var syntax = Inspection.IdentifySqlSyntax(parseState, ctx.Operation, out var caseSensitive);
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

            if (parseState.Options.TryGetDebugModeFlags(out var debugModeFlags))
            {
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
}
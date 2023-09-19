﻿using Dapper.Internal;
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
using ModeFlags = Dapper.SqlAnalysis.TSqlProcessor.ModeFlags;

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

        private void ValidateDapperMethod(in OperationAnalysisContext ctx, IOperation sqlSource, OperationFlags flags)
        {
            if (ctx.Operation is not IInvocationOperation invoke) return;

            var location = invoke.GetMemberLocation();
            var parseState = new ParseState(ctx);

            if (Inspection.IsEnabled(in parseState, ctx.Operation, Types.DapperAotAttribute, out var aotAttribExists))
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

            var modeFlags = ModeFlags.None;
            // if (caseSensitive) modeFlags |= ModeFlags.CaseSensitive;
            if ((flags & OperationFlags.BindResultsByName) != 0) modeFlags |= ModeFlags.ValidateSelectNames;
            if ((flags & OperationFlags.SingleRow) != 0) modeFlags |= ModeFlags.SingleRow;
            if ((flags & OperationFlags.AtMostOne) != 0) modeFlags |= ModeFlags.AtMostOne;
            if ((flags & OperationFlags.Scalar) != 0)
            {
                modeFlags |= ModeFlags.ExpectQuery | ModeFlags.SingleRow | ModeFlags.SingleQuery | ModeFlags.SingleColumn;
            }
            else if ((flags & OperationFlags.Query) != 0)
            {
                modeFlags |= ModeFlags.ExpectQuery;
                if ((flags & OperationFlags.QueryMultiple) == 0) modeFlags |= ModeFlags.SingleQuery;
            }
            else if ((flags & (OperationFlags.Execute)) != 0) modeFlags |= ModeFlags.ExpectNoQuery;

            ValidateSql(ctx, sqlSource, modeFlags, location);

            ValidateSurroundingLinqUsage(ctx, flags);
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
            var flags = ModeFlags.None;
            ValidateSql(ctx, sqlSource, flags);
        }

        private static void ValidatePropertyUsage(in OperationAnalysisContext ctx, IOperation sqlSource, bool isCommand)
        {
            var flags = ModeFlags.None;
            if (isCommand)
            {
                // TODO: check other parameters for special markers like command type?
                // check the method being invoked - scalar, reader, etc
            }
            ValidateSql(ctx, sqlSource, flags);
        }

        private static readonly Regex HasWhitespace = new Regex(@"\s", RegexOptions.Compiled | RegexOptions.Multiline);

        private static void ValidateSql(in OperationAnalysisContext ctx, IOperation sqlSource, ModeFlags flags, Location? location = null)
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

            if (caseSensitive) flags |= ModeFlags.CaseSensitive;

            // can we get the SQL itself?
            if (!Inspection.TryGetConstantValueWithSyntax(sqlSource, out string? sql, out var sqlSyntax)) return;
            if (string.IsNullOrWhiteSpace(sql) || !HasWhitespace.IsMatch(sql)) return; // need non-trivial content to validate

            location ??= ctx.Operation.Syntax.GetLocation();

            if (parseState.Options.TryGetDebugModeFlags(out var debugModeFlags))
            {
                flags |= debugModeFlags;
            }

            try
            {
                switch (syntax)
                {
                    case SqlSyntax.SqlServer:

                        var proc = new OperationAnalysisContextTSqlProcessor(ctx, null, flags, location, sqlSyntax);
                        proc.Execute(sql!, members: default);
                        // paramMembers);
                        //parseFlags = proc.Flags;
                        //paramNames = (from var in proc.Variables
                        //              where var.IsParameter
                        //              select var.Name.StartsWith("@") ? var.Name.Substring(1) : var.Name
                        //              ).ToImmutableHashSet();
                        //diagnostics = proc.DiagnosticsObject;
                        break;
                }
            }
            catch (Exception ex)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.GeneralSqlError, location, ex.Message));
            }
        }
    }
}
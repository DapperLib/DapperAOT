using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
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

        // respond to method usages
        context.RegisterOperationAction(OnOperation,
            OperationKind.Invocation, OperationKind.SimpleAssignment);
    }

    private void OnOperation(OperationAnalysisContext ctx)
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
                        var kind = Inspection.IsSupportedDapperMethod(invoke, out var dapperFlags);
                        switch (kind)
                        {
                            case DapperMethodKind.DapperSupported:
                            case DapperMethodKind.DapperUnsupported:
                                ValidateDapperMethod(ctx, invoke.Arguments[index], dapperFlags);
                                break;
                            case DapperMethodKind.NotDapper:
                            default:
                                ValidateParameterUsage(ctx, invoke.Arguments[index]);
                                break;
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

    private void ValidateDapperMethod(OperationAnalysisContext ctx, IOperation sqlSource, OperationFlags flags)
    {
        Location? location = null;
        var node = ctx.Operation.Syntax.ChildNodes().FirstOrDefault();
        if (node.IsMemberAccess())
        {
            location = node.ChildNodes().Skip(1).FirstOrDefault()?.GetLocation();
        }
        location ??= ctx.Operation.Syntax.GetLocation();

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

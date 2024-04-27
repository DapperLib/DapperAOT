using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Diagnostics;

namespace Dapper.Internal;

internal sealed class OperationAnalysisContextTSqlProcessor : DiagnosticTSqlProcessor
{
    private readonly OperationAnalysisContext _ctx;
    public OperationAnalysisContextTSqlProcessor(in OperationAnalysisContext ctx, ITypeSymbol? parameterType, SqlParseInputFlags flags,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax)
        :base(parameterType, flags, location, sqlSyntax)
    {
        _ctx = ctx;
    }
    protected override void OnDiagnostic(Diagnostic diagnostic) => _ctx.ReportDiagnostic(diagnostic);
}
internal abstract class DiagnosticTSqlProcessor : TSqlProcessor
{
    private readonly Microsoft.CodeAnalysis.Location? _location;
    private readonly SyntaxToken? _sourceToken;

    public DiagnosticTSqlProcessor(ITypeSymbol? parameterType, SqlParseInputFlags flags,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax) : base(flags)
    {
        _location = sqlSyntax?.GetLocation() ?? location;
        if (sqlSyntax.TryGetLiteralToken(out var token))
        {
            _sourceToken = token;
        }
    }

    protected abstract void OnDiagnostic(Diagnostic diagnostic);
    private void OnDiagnostic(DiagnosticDescriptor descriptor, Location location, params object[] args)
        => OnDiagnostic(Diagnostic.Create(descriptor, GetLocation(location), args));

    private Microsoft.CodeAnalysis.Location? GetLocation(in Location location)
    {
        if (_sourceToken is not null && !location.IsZero)
        {
            return _sourceToken.GetValueOrDefault().ComputeLocation(in location);
        }
        // just point to the operation
        return _location;
    }

    protected override void OnError(string error, in Location location)
    {
        Debug.Fail("unhandled error: " + error);
        OnDiagnostic(DapperAnalyzer.Diagnostics.GeneralSqlError, location, error);
    }

    protected override void OnAdditionalBatch(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.MultipleBatches, location);

    protected override void OnDuplicateVariableDeclaration(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.DuplicateVariableDeclaration, variable.Location, variable.Name);

    protected override void OnVariableParameterConflict(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.VariableParameterConflict, variable.Location, variable.Name);

    protected override void OnExecComposedSql(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.ExecComposedSql, location);
    protected override void OnSelectScopeIdentity(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectScopeIdentity, location);
    protected override void OnGlobalIdentity(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.GlobalIdentity, location);

    protected override void OnNullLiteralComparison(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.NullLiteralComparison, location);

    protected override void OnParseError(ParseError error, Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.ParseError, location, error.Number, error.Message);

    protected override void OnPseudoPositionalParameter(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.PseudoPositionalParameter, location);

    protected override void OnScalarVariableUsedAsTable(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.ScalarVariableUsedAsTable, variable.Location, variable.Name);

    protected override void OnTableVariableUsedAsScalar(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.TableVariableUsedAsScalar, variable.Location, variable.Name);

    protected override void OnVariableAccessedBeforeAssignment(Variable variable)
        => OnDiagnostic(variable.IsTable
            ? DapperAnalyzer.Diagnostics.TableVariableAccessedBeforePopulate
            : DapperAnalyzer.Diagnostics.VariableAccessedBeforeAssignment, variable.Location, variable.Name);

    protected override void OnVariableNotDeclared(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.VariableNotDeclared, variable.Location, variable.Name);

    protected override void OnVariableAccessedBeforeDeclaration(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.VariableAccessedBeforeDeclaration, variable.Location, variable.Name);

    protected override void OnVariableValueNotConsumed(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.VariableValueNotConsumed, variable.Location, variable.Name);

    protected override void OnTableVariableOutputParameter(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.TableVariableOutputParameter, variable.Location, variable.Name);

    protected override void OnInsertColumnMismatch(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.InsertColumnsMismatch, location);
    protected override void OnInsertColumnsNotSpecified(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.InsertColumnsNotSpecified, location);
    protected override void OnInsertColumnsUnbalanced(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.InsertColumnsUnbalanced, location);
    protected override void OnSelectStar(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectStar, location);
    protected override void OnSelectEmptyColumnName(Location location, int column)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectEmptyColumnName, location, column);
    protected override void OnSelectDuplicateColumnName(Location location, string name)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectDuplicateColumnName, location, name);
    protected override void OnSelectAssignAndRead(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectAssignAndRead, location);

    protected override void OnUpdateWithoutWhere(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.UpdateWithoutWhere, location);
    protected override void OnDeleteWithoutWhere(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.DeleteWithoutWhere, location);

    protected override void OnFromMultiTableMissingAlias(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.FromMultiTableMissingAlias, location);
    protected override void OnFromMultiTableUnqualifiedColumn(Location location, string name)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.FromMultiTableUnqualifiedColumn, location, name);

    protected override void OnInvalidDatepartToken(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.InvalidDatepartToken, location);

    protected override void OnNonIntegerTop(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.NonIntegerTop, location);
    protected override void OnNonPositiveTop(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.NonPositiveTop, location);
    protected override void OnNonPositiveFetch(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.NonPositiveFetch, location);
    protected override void OnNegativeOffset(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.NegativeOffset, location);
    protected override void OnSelectFirstTopError(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectFirstTopError, location);
    protected override void OnSelectSingleRowWithoutWhere(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectSingleRowWithoutWhere, location);
    protected override void OnSelectAggregateAndNonAggregate(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectAggregateMismatch, location);
    protected override void OnSelectSingleTopError(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SelectSingleTopError, location);

    protected override void OnSimplifyExpression(Location location, string value)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.SimplifyExpression, location, value);

    protected override void OnTopWithOffset(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.TopWithOffset, location);

    protected override void OnUnusedParameter(Variable variable)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.UnusedParameter, variable.Location, variable.Name);

    protected override void OnDivideByZero(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.DivideByZero, location);

    protected override void OnInvalidNullExpression(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.InvalidNullExpression, location);

    protected override void OnTrivialOperand(Location location)
        => OnDiagnostic(DapperAnalyzer.Diagnostics.TrivialOperand, location);
}

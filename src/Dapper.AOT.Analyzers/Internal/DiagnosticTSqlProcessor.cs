using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location;
    private readonly LiteralExpressionSyntax? _literal;
    public object? DiagnosticsObject => _diagnostics;

    public DiagnosticTSqlProcessor(bool caseSensitive, object? diagnostics,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax) : base(caseSensitive)
    {
        _diagnostics = diagnostics;
        _location = sqlSyntax?.GetLocation() ?? location;

        switch (sqlSyntax)
        {
            case LiteralExpressionSyntax direct:
                _literal = direct;
                break;
            case VariableDeclaratorSyntax decl when decl.Initializer?.Value is LiteralExpressionSyntax indirect:
                _literal = indirect;
                break;
           // other interesting possibilities include InterpolatedStringExpression, AddExpression
        }
    }

    private Microsoft.CodeAnalysis.Location? GetLocation(in Location location)
    {
        if (_literal is not null)
        {
            return LiteralLocationHelper.ComputeLocation(_literal.Token, in location);
        }
        // just point to the operation
        return _location;
    }

    

    private void AddDiagnostic(DiagnosticDescriptor diagnostic, in Location location, params object?[]? args)
    {
        Diagnostics.Add(ref _diagnostics, Diagnostic.Create(diagnostic, GetLocation(location), args));
    }
    protected override void OnError(string error, in Location location)
        => AddDiagnostic(Diagnostics.SqlError, location, error, location.Line, location.Column);

    protected override void OnAdditionalBatch(Location location)
        => AddDiagnostic(Diagnostics.MultipleBatches, location, location.Line, location.Column);

    protected override void OnDuplicateVariableDeclaration(Variable variable)
        => AddDiagnostic(Diagnostics.DuplicateVariableDeclaration, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnExecVariable(Location location)
        => AddDiagnostic(Diagnostics.ExecVariable, location, location.Line, location.Column);
    protected override void OnSelectScopeIdentity(Location location)
        => AddDiagnostic(Diagnostics.SelectScopeIdentity, location, location.Line, location.Column);
    protected override void OnGlobalIdentity(Location location)
        => AddDiagnostic(Diagnostics.GlobalIdentity, location, location.Line, location.Column);

    protected override void OnNullLiteralComparison(Location location)
        => AddDiagnostic(Diagnostics.NullLiteralComparison, location, location.Line, location.Column);

    protected override void OnParseError(ParseError error, Location location)
        => AddDiagnostic(Diagnostics.ParseError, location, error.Message, error.Number, location.Line, location.Column);

    protected override void OnScalarVariableUsedAsTable(Variable variable)
        => AddDiagnostic(Diagnostics.ScalarVariableUsedAsTable, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnTableVariableUsedAsScalar(Variable variable)
        => AddDiagnostic(Diagnostics.TableVariableUsedAsScalar, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableAccessedBeforeAssignment(Variable variable)
        => AddDiagnostic(variable.IsTable ? Diagnostics.TableVariableAccessedBeforePopulate : Diagnostics.VariableAccessedBeforeAssignment, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableAccessedBeforeDeclaration(Variable variable)
        => AddDiagnostic(Diagnostics.VariableAccessedBeforeDeclaration, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);
}

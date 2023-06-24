using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location;
    private readonly SyntaxNode? _sqlSyntax;
    public object? DiagnosticsObject => _diagnostics;
    private readonly Action<DiagnosticSeverity, string>? Log;

    public DiagnosticTSqlProcessor(bool caseSensitive, object? diagnostics,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax, Action<DiagnosticSeverity, string>? log) : base(caseSensitive)
    {
        _diagnostics = diagnostics;
        _location = sqlSyntax?.GetLocation() ?? location;
        Log = log;

        _sqlSyntax = sqlSyntax; // default to simple
        switch (sqlSyntax)
        {
            case LiteralExpressionSyntax direct:
                var token = direct.Token;
                var loc = direct.GetLocation().GetLineSpan().StartLinePosition;
                log?.Invoke(DiagnosticSeverity.Info, $"direct {token.Kind()} L{loc.Line}C{loc.Character}: '{token.Text}'");
                break;
            case VariableDeclaratorSyntax decl when decl.Initializer?.Value is LiteralExpressionSyntax indirect:
                token = indirect.Token;
                loc = indirect.GetLocation().GetLineSpan().StartLinePosition;
                log?.Invoke(DiagnosticSeverity.Info, $"indirect {token.Kind()} L{loc.Line}C{loc.Character}: '{token.Text}'");
                break;
        }
    }
    public override bool Execute(string sql)
    {
        Log?.Invoke(DiagnosticSeverity.Info, sql);
        return base.Execute(sql);
    }

    private Microsoft.CodeAnalysis.Location? GetLocation(in Location location)
    {
        if (_sqlSyntax is not null)
        {
            //if (location.Line == 1)
            //{
            //    // for very simple cases, we'll try to give a good indication, but it get's too weird
            //    // for multi-line SQL, given how constant strings might be composed
            //    var newSpan = new TextSpan(_sqlLocation.SourceSpan.Start + location.Offset, location.Length);
            //    return Microsoft.CodeAnalysis.Location.Create(_sqlLocation.SourceTree, newSpan);
            //}
            // just point at the SQL with a vague hand-wave
            return _sqlSyntax.GetLocation();
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

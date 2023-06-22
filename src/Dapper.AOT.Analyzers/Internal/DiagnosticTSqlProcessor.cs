using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Dapper.SqlAnalysis.TSqlProcessor;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location, _sqlLocation;
    public object? DiagnosticsObject => _diagnostics;
    public DiagnosticTSqlProcessor(bool caseSensitive, object? diagnostics,
        Microsoft.CodeAnalysis.Location? location, Microsoft.CodeAnalysis.Location? sqlLocation) : base(caseSensitive)
    {
        _diagnostics = diagnostics;
        _location = location;
        _sqlLocation = sqlLocation;
    }
    private Microsoft.CodeAnalysis.Location? GetLocation(in Location location)
    {
        if (_sqlLocation is { SourceTree: not null })
        {
            if (location.Line == 1)
            {
                // for very simple cases, we'll try to give a good indication, but it get's too weird
                // for multi-line SQL, given how constant strings might be composed
                var newSpan = new TextSpan(_sqlLocation.SourceSpan.Start + location.Offset, location.Length);
                return Microsoft.CodeAnalysis.Location.Create(_sqlLocation.SourceTree, newSpan);
            }
            // just point at the SQL with a vague hand-wave
            return _sqlLocation;
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

    protected override void OnNewSyntaxRecommendation(string used, string recommended, Location location, bool strong)
        => AddDiagnostic(strong ? Diagnostics.StrongSyntaxRecommendation : Diagnostics.SyntaxRecommendation, location, recommended, used, location.Line, location.Column);

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

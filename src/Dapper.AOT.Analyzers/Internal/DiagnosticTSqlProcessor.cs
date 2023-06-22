using Dapper.CodeAnalysis;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location;
    public object? DiagnosticsObject => _diagnostics;
    public DiagnosticTSqlProcessor(bool caseSensitive, object? diagnostics, Microsoft.CodeAnalysis.Location? location) : base(caseSensitive)
    {
        _diagnostics = diagnostics;
        _location = location;
    }
    protected override void OnError(string error, Location location)
    {
        Diagnostics.Add(ref _diagnostics, Diagnostic.Create(Diagnostics.SqlError, _location, $"{error} (L{location.Line} C{location.Column})"));
    }
}

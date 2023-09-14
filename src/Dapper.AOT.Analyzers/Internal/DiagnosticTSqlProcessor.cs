using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Data;
using static Dapper.SqlAnalysis.TSqlProcessor;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location;
    private readonly LiteralExpressionSyntax? _literal;
    public object? DiagnosticsObject => _diagnostics;
    private readonly ITypeSymbol? _parameterType;
    public DiagnosticTSqlProcessor(ITypeSymbol? parameterType, ModeFlags flags, object? diagnostics,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax) : base(flags)
    {
        _diagnostics = diagnostics;
        _location = sqlSyntax?.GetLocation() ?? location;
        _parameterType = parameterType;

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

    protected override void OnVariableNotDeclared(Variable variable)
        => AddDiagnostic(Diagnostics.VariableNotDeclared, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableAccessedBeforeDeclaration(Variable variable)
        => AddDiagnostic(Diagnostics.VariableAccessedBeforeDeclaration, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableValueNotConsumed(Variable variable)
        => AddDiagnostic(Diagnostics.VariableValueNotConsumed, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnTableVariableOutputParameter(Variable variable)
        => AddDiagnostic(Diagnostics.TableVariableOutputParameter, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnInsertColumnMismatch(Location location)
        => AddDiagnostic(Diagnostics.InsertColumnsMismatch, location, location.Line, location.Column);
    protected override void OnInsertColumnsNotSpecified(Location location)
        => AddDiagnostic(Diagnostics.InsertColumnsNotSpecified, location, location.Line, location.Column);
    protected override void OnInsertColumnsUnbalanced(Location location)
        => AddDiagnostic(Diagnostics.InsertColumnsUnbalanced, location, location.Line, location.Column);
    protected override void OnSelectStar(Location location)
        => AddDiagnostic(Diagnostics.SelectStar, location, location.Line, location.Column);
    protected override void OnSelectEmptyColumnName(Location location, int column)
        => AddDiagnostic(Diagnostics.SelectEmptyColumnName, location, column, location.Line, location.Column);
    protected override void OnSelectDuplicateColumnName(Location location, string name)
        => AddDiagnostic(Diagnostics.SelectDuplicateColumnName, location, name, location.Line, location.Column);
    protected override void OnSelectAssignAndRead(Location location)
        => AddDiagnostic(Diagnostics.SelectAssignAndRead, location, location.Line, location.Column);

    protected override void OnUpdateWithoutWhere(Location location)
        => AddDiagnostic(Diagnostics.UpdateWithoutWhere, location, location.Line, location.Column);
    protected override void OnDeleteWithoutWhere(Location location)
        => AddDiagnostic(Diagnostics.DeleteWithoutWhere, location, location.Line, location.Column);

    protected override bool TryGetParameter(string name, out ParameterDirection direction)
    {
        // we have knowledge of the type system; use it

        if (_parameterType is null)
        {
            // no parameter
            direction = default;
            return false;
        }

        if (Inspection.IsMissingOrObjectOrDynamic(_parameterType))
        {
            // dynamic
            direction = ParameterDirection.Input;
            return true;
        }

        if (!string.IsNullOrEmpty(name) && name[0] == '@')
        {
            name = name.Substring(1);
        }
        if (!string.IsNullOrWhiteSpace(name))
        {
            bool caseSensitive = CaseSensitive;
            foreach (var member in Inspection.GetMembers(_parameterType))
            {
                if (caseSensitive ? member.DbName == name : string.Equals(member.DbName, name, StringComparison.OrdinalIgnoreCase))
                {
                    direction = member.Direction;
                    return true;
                }
            }
        }

        // nope
        direction = default;
        return false;
    }
}

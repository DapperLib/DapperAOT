﻿using Dapper.CodeAnalysis;
using Dapper.Internal.Roslyn;
using Dapper.SqlAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Data;
using System.Diagnostics;

namespace Dapper.Internal;

internal class DiagnosticTSqlProcessor : TSqlProcessor
{
    private object? _diagnostics;
    private readonly Microsoft.CodeAnalysis.Location? _location;
    private readonly LiteralExpressionSyntax? _literal;
    public object? DiagnosticsObject => _diagnostics;
    private readonly ITypeSymbol? _parameterType;
    private readonly bool _assumeParameterExists;
    public DiagnosticTSqlProcessor(ITypeSymbol? parameterType, ModeFlags flags, object? diagnostics,
        Microsoft.CodeAnalysis.Location? location, SyntaxNode? sqlSyntax) : base(flags)
    {
        _diagnostics = diagnostics;
        _location = sqlSyntax?.GetLocation() ?? location;
        _parameterType = parameterType;
        _assumeParameterExists = Inspection.IsMissingOrObjectOrDynamic(_parameterType) || IsDyanmicParameters(parameterType);
        if (_assumeParameterExists) AddFlags(ParseFlags.DynamicParameters);
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

    public override void Reset()
    {
        base.Reset();
        if (_assumeParameterExists) AddFlags(ParseFlags.DynamicParameters);
    }

    static bool IsDyanmicParameters(ITypeSymbol? type)
    {
        if (type is null || type.SpecialType != SpecialType.None) return false;
        if (Inspection.IsBasicDapperType(type, Types.DynamicParameters)
            || Inspection.IsNestedSqlMapperType(type, Types.IDynamicParameters, TypeKind.Interface)) return true;
        foreach (var i in type.AllInterfaces)
        {
            if (Inspection.IsNestedSqlMapperType(i, Types.IDynamicParameters, TypeKind.Interface)) return true;
        }
        return false;
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
        DiagnosticsBase.Add(ref _diagnostics, Diagnostic.Create(diagnostic, GetLocation(location), args));
    }
    protected override void OnError(string error, in Location location)
    {
        Debug.Fail("unhandled error: " + error);
        AddDiagnostic(DapperInterceptorGenerator.Diagnostics.GeneralSqlError, location, error, location.Line, location.Column);
    }

    protected override void OnAdditionalBatch(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.MultipleBatches, location, location.Line, location.Column);

    protected override void OnDuplicateVariableDeclaration(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.DuplicateVariableDeclaration, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnExecVariable(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.ExecVariable, location, location.Line, location.Column);
    protected override void OnSelectScopeIdentity(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectScopeIdentity, location, location.Line, location.Column);
    protected override void OnGlobalIdentity(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.GlobalIdentity, location, location.Line, location.Column);

    protected override void OnNullLiteralComparison(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.NullLiteralComparison, location, location.Line, location.Column);

    protected override void OnParseError(ParseError error, Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.ParseError, location, error.Message, error.Number, location.Line, location.Column);

    protected override void OnScalarVariableUsedAsTable(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.ScalarVariableUsedAsTable, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnTableVariableUsedAsScalar(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.TableVariableUsedAsScalar, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableAccessedBeforeAssignment(Variable variable)
        => AddDiagnostic(variable.IsTable
            ? DapperInterceptorGenerator.Diagnostics.TableVariableAccessedBeforePopulate
            : DapperInterceptorGenerator.Diagnostics.VariableAccessedBeforeAssignment, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableNotDeclared(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.VariableNotDeclared, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableAccessedBeforeDeclaration(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.VariableAccessedBeforeDeclaration, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnVariableValueNotConsumed(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.VariableValueNotConsumed, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnTableVariableOutputParameter(Variable variable)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.TableVariableOutputParameter, variable.Location, variable.Name, variable.Location.Line, variable.Location.Column);

    protected override void OnInsertColumnMismatch(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.InsertColumnsMismatch, location, location.Line, location.Column);
    protected override void OnInsertColumnsNotSpecified(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.InsertColumnsNotSpecified, location, location.Line, location.Column);
    protected override void OnInsertColumnsUnbalanced(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.InsertColumnsUnbalanced, location, location.Line, location.Column);
    protected override void OnSelectStar(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectStar, location, location.Line, location.Column);
    protected override void OnSelectEmptyColumnName(Location location, int column)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectEmptyColumnName, location, column, location.Line, location.Column);
    protected override void OnSelectDuplicateColumnName(Location location, string name)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectDuplicateColumnName, location, name, location.Line, location.Column);
    protected override void OnSelectAssignAndRead(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectAssignAndRead, location, location.Line, location.Column);

    protected override void OnUpdateWithoutWhere(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.UpdateWithoutWhere, location, location.Line, location.Column);
    protected override void OnDeleteWithoutWhere(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.DeleteWithoutWhere, location, location.Line, location.Column);

    protected override void OnFromMultiTableMissingAlias(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.FromMultiTableMissingAlias, location, location.Line, location.Column);
    protected override void OnFromMultiTableUnqualifiedColumn(Location location, string name)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.FromMultiTableUnqualifiedColumn, location, name, location.Line, location.Column);
    protected override void OnNonIntegerTop(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.NonIntegerTop, location, location.Line, location.Column);
    protected override void OnNonPositiveTop(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.NonPositiveTop, location, location.Line, location.Column);
    protected override void OnSelectFirstTopError(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectFirstTopError, location, location.Line, location.Column);
    protected override void OnSelectSingleRowWithoutWhere(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectSingleRowWithoutWhere, location, location.Line, location.Column);
    protected override void OnSelectSingleTopError(Location location)
        => AddDiagnostic(DapperInterceptorGenerator.Diagnostics.SelectSingleTopError, location, location.Line, location.Column);

    protected override bool TryGetParameter(string name, out ParameterDirection direction)
    {
        // we have knowledge of the type system; use it

        if (_parameterType is null)
        {
            // no parameter
            direction = default;
            return false;
        }

        if (_assumeParameterExists) // dynamic, etc
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

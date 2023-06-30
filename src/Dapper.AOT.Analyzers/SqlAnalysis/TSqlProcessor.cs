using Microsoft.CodeAnalysis;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Dapper.SqlAnalysis;

internal class TSqlProcessor
{
    [Flags]
    internal enum VariableFlags
    {
        None = 0,
        NoValue = 1 << 0,
        Parameter = 1 << 1,
        Table = 1 << 2,
        Unconsumed = 1 << 3,
        OutputParameter = 1 << 4,
    }
    private readonly VariableTrackingVisitor _visitor;
    protected bool CaseSensitive => _visitor.CaseSensitive;
    public TSqlProcessor(bool caseSensitive = false, Action<string>? log = null)
    {
        _visitor = log is null ? new VariableTrackingVisitor(caseSensitive, this) : new LoggingVariableTrackingVisitor(caseSensitive, this, log);
    }
    public virtual bool Execute(string sql)
    {
        Reset();
        var parser = new TSql160Parser(true, SqlEngineType.All);
        TSqlFragment tree;
        using (var reader = new StringReader(sql))
        {
            tree = parser.Parse(reader, out var errors);
            if (errors is not null && errors.Count != 0)
            {
                Flags |= ParseFlags.SyntaxError;
                foreach (var error in errors)
                {
                    OnParseError(error, new Location(error.Line, error.Column, error.Offset, 0));
                }
            }
        }
        tree.Accept(_visitor);
        foreach (var variable in _visitor.Variables)
        {
            if (variable.IsUnconsumed && !variable.IsTable && !variable.IsOutputParameter)
            {
                if (_visitor.AssignmentTracking) OnVariableValueNotConsumed(variable);
            }
        }
        return true;
    }

    public IEnumerable<Variable> Variables => _visitor.Variables;

    public ParseFlags Flags { get; private set; }
    public virtual void Reset()
    {
        Flags = ParseFlags.Reliable;
        _visitor.Reset();
    }

    protected virtual void OnError(string error, in Location location) { }

    protected virtual void OnParseError(ParseError error, Location location)
        => OnError($"{error.Message} (#{error.Number})", location);

    protected virtual void OnVariableAccessedBeforeDeclaration(Variable variable)
        => OnError($"Variable {variable.Name} accessed before declaration", variable.Location);

    protected virtual void OnVariableValueNotConsumed(Variable variable)
        => OnError($"Variable {variable.Name} has a value that is not consumed", variable.Location);

    protected virtual void OnVariableAccessedBeforeAssignment(Variable variable)
        => OnError($"Variable {variable.Name} accessed before being {(variable.IsTable ? "populated" : "assigned")}", variable.Location);

    protected virtual void OnVariableNotDeclared(Variable variable)
        => OnError($"Variable {variable.Name} is not declared and no corresponding parameter exists", variable.Location);

    protected virtual void OnDuplicateVariableDeclaration(Variable variable)
        => OnError($"Variable {variable.Name} is declared multiple times", variable.Location);

    protected virtual void OnScalarVariableUsedAsTable(Variable variable)
        => OnError($"Scalar variable {variable.Name} is used like a table", variable.Location);

    protected virtual void OnTableVariableUsedAsScalar(Variable variable)
        => OnError($"Table variable {variable.Name} is used like a scalar", variable.Location);

    protected virtual void OnNullLiteralComparison(Location location)
        => OnError($"Null literals should not be used in binary comparisons; prefer 'is null' and 'is not null'", location);

    protected virtual void OnAdditionalBatch(Location location)
        => OnError($"Multiple batches are not permitted", location);

    protected virtual void OnGlobalIdentity(Location location)
        => OnError($"@@identity should not be used; use SCOPE_IDENTITY() instead", location);

    protected virtual void OnSelectScopeIdentity(Location location)
        => OnError($"Consider OUTPUT INSERTED.yourid on the INSERT instead of SELECT SCOPE_IDENTITY()", location);

    protected virtual void OnExecVariable(Location location)
        => OnError($"EXEC with composed SQL may be susceptible to SQL injection; consider EXEC sp_executesql with parameters", location);

    protected virtual void OnTableVariableOutputParameter(Variable variable)
        => OnError($"Table variable {variable.Name} cannot be used as an output parameter", variable.Location);

    internal readonly struct Location
    {
        public Location(TSqlFragment source) : this()
        {
            Line = source.StartLine;
            Column = source.StartColumn;
            Offset = source.StartOffset;
            Length = source.FragmentLength;
        }

        public Location(int line, int column, int offset, int length)
        {
            Line = line;
            Column = column;
            Offset = offset;
            Length = length;
        }

        public readonly int Line, Column, Offset, Length;

        public override string ToString() => $"L{Line} C{Column}";

    }
    internal readonly struct Variable
    {
        public readonly Location Location;
        public readonly string Name;
        public readonly VariableFlags Flags;
        public override string ToString() => Name;


        public bool IsTable => (Flags & VariableFlags.Table) != 0;
        public bool NoValue => (Flags & VariableFlags.NoValue) != 0;
        public bool IsUnconsumed => (Flags & VariableFlags.Unconsumed) != 0;
        public bool IsParameter => (Flags & VariableFlags.Parameter) != 0;
        public bool IsOutputParameter => (Flags & VariableFlags.OutputParameter) != 0;

        public Variable(Identifier identifier, VariableFlags flags)
        {
            Flags = flags;
            Name = identifier.Value;
            Location = new Location(identifier);
        }
        public Variable(VariableReference reference, VariableFlags flags)
        {
            Flags = flags;
            Name = reference.Name;
            Location = new Location(reference);
        }

        private Variable(scoped in Variable source, VariableFlags flags)
        {
            this = source;
            Flags = flags;
        }
        private Variable(scoped in Variable source, Location location)
        {
            this = source;
            Location = location;
        }

        public Variable WithConsumed() => new(in this, (Flags & ~VariableFlags.Unconsumed));
        public Variable WithUnconsumedValue() => new(in this, (Flags & ~VariableFlags.NoValue) | VariableFlags.Unconsumed);
        public Variable WithFlags(VariableFlags flags) => new(in this, flags);

        public Variable WithLocation(TSqlFragment node) => new(in this, new Location(node));
    }
    class LoggingVariableTrackingVisitor : VariableTrackingVisitor
    {
        private readonly Action<string> log;
        public LoggingVariableTrackingVisitor(bool caseSensitive, TSqlProcessor parser, Action<string> log) : base(caseSensitive, parser)
        {
            this.log = log;
        }
        public override void Visit(TSqlFragment node)
        {
            switch (node)
            {
                case VariableReference var:
                    log(node.GetType().Name + ": " + var.Name);
                    break;
                case VariableTableReference tvar:
                    log(node.GetType().Name + ": " + tvar.Variable?.Name);
                    break;
                case SqlDataTypeReference sdt:
                    log(node.GetType().Name + ": " + string.Join(".", sdt.Name.Identifiers.Select(x => x.Value)));
                    break;
                case IntegerLiteral liti:
                    log(node.GetType().Name + ": " + liti.Value);
                    break;
                case StringLiteral lits:
                    log(node.GetType().Name + ": '" + lits.Value + "'");
                    break;
                case SchemaObjectName obj:
                    log(node.GetType().Name + ": " + string.Join(".", obj.Identifiers.Select(x => x.Value)));
                    break;
                case Identifier id:
                    log(node.GetType().Name + ": " + id.Value);
                    break;
                case GlobalVariableExpression gve:
                    log(node.GetType().Name + ": " + gve.Name);
                    break;
                default:
                    log(node.GetType().Name);
                    break;
            }
            base.Visit(node);
        }
    }
    class VariableTrackingVisitor : TSqlFragmentVisitor
    {
        // important note for anyone maintaining this;
        //
        // the way the machinery works is:
        // Accept calls ExplicitVisit; on any node, ExplicitVisit calls Visit on the current node,
        // then calls node.AcceptChildren, which (for each child element) calls child.Accept(this)
        //
        // what this means is:
        //
        // - for simple "I spotted a thing" rules, you can just override Visit, add your logic,
        //   and call base.Visit() - how we spot @@identity in GlobalVariableExpression is an example
        //
        // - if you need to add some side-effect *before or after* the standard processing, you can override
        //   ExplicitVisit chaining base.ExplicitVisit, adding your logic; ExecuteParameter "output" params
        //   (marking them as assigned so we don't report them as errors) is an example
        //
        // - if you need to *change the order of evaluation*, bypass nodes, etc; then you will need to
        //   override ExplicitVisit, but look at what node.AcceptChildren does; be sure to call
        //   Visit(node) and replicate any unrelated old behaviour from node.AcceptChildren, but **DO NOT**
        //   call base.ExplicitVisit; VariableTableReference is an example, omitting node.Variable?.Accept(this)
        //   to avoid a problem; also, be sure to think "nulls", so: ?.Accept(this), if you're not sure

        public VariableTrackingVisitor(bool caseSensitive, TSqlProcessor parser)
        {
            variables = caseSensitive ? new(StringComparer.Ordinal) : new(StringComparer.OrdinalIgnoreCase);
            this.parser = parser;
        }

        public bool CaseSensitive => ReferenceEquals(variables.Comparer, StringComparer.Ordinal);

        private readonly Dictionary<string, Variable> variables;
        private int batchCount;
        private readonly TSqlProcessor parser;
        public bool AssignmentTracking { get; private set; }

        public IEnumerable<Variable> Variables => variables.Values;

        public virtual void Reset()
        {
            AssignmentTracking = true;
            variables.Clear();
            batchCount = 0;
        }

        public override void Visit(TSqlBatch node)
        {
            if (++batchCount >= 2)
            {
                parser.OnAdditionalBatch(new Location(node));
            }
            base.Visit(node);
        }

        private void OnDeclare(Variable variable)
        {
            if (variables.TryGetValue(variable.Name, out var existing))
            {
                if (existing.IsParameter)
                {
                    // we previously assumed it was a parameter, but actually it was accessed before declaration
                    variables[variable.Name] = existing.WithFlags(variable.Flags);
                    // but the *original* one was accessed invalidly
                    if (AssignmentTracking) parser.OnVariableAccessedBeforeDeclaration(existing);
                }
                else
                {
                    // two definitions? yeah, that's wrong
                    parser.OnDuplicateVariableDeclaration(variable);
                }
            }
            else
            {
                variables.Add(variable.Name, variable);
            }
        }
        public override void ExplicitVisit(DeclareVariableElement node)
        {
            Visit(node);
            string? name = null;
            if (node.VariableName is not null)
            {
                OnDeclare(new(node.VariableName, VariableFlags.NoValue));
                name = node.VariableName.Value;
                node.VariableName.Accept(this);
            }
            node.DataType?.Accept(this);
            node.Nullable?.Accept(this);
            // assign if there is a value
            if (node.Value is not null)
            {
                node.Value.Accept(this);
                // mark assigned
                if (name is not null)
                {
                    variables[name] = variables[name].WithUnconsumedValue();
                }
            }
        }

        public override void ExplicitVisit(OutputIntoClause node)
        {
            Visit(node);
            foreach (var col in node.SelectColumns)
            {
                col.Accept(this);
            }
            foreach (var col in node.IntoTableColumns)
            {
                col.Accept(this);
            }
            if (node.IntoTable is VariableTableReference tableVar)
            {
                MarkAssigned(tableVar.Variable, true);
                // but do *NOT* visit, as that would trigger a scalar/table warning
            }
            else
            {
                node.IntoTable?.Accept(this);
            }
        }

        public override void Visit(ReturnStatement node)
        {
            parser.Flags |= ParseFlags.Return;
            base.Visit(node);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            Visit(node);
            node.Identifier?.Accept(this);
            foreach (var p in node.Parameters)
            {
                p.Accept(this);
            }
            node.Expression?.Accept(this);
            node.CursorDefinition?.Accept(this);
            if (node.Variable is not null)
            {
                MarkAssigned(node.Variable, false);
                // but don't actually visit
            }
        }

        public override void Visit(BooleanComparisonExpression node)
        {
            if (node.FirstExpression is NullLiteral)
            {
                parser.OnNullLiteralComparison(new Location(node.FirstExpression));
            }
            if (node.SecondExpression is NullLiteral)
            {
                parser.OnNullLiteralComparison(new Location(node.SecondExpression));
            }
            base.Visit(node);
        }

        public override void ExplicitVisit(QuerySpecification node)
        {
            base.ExplicitVisit(node);
            foreach (var el in node.SelectElements)
            {
                if (el is SelectSetVariable setVar && setVar.Variable is not null)
                {
                    // assign *after* the select
                    MarkAssigned(setVar.Variable, false);
                }
            }
        }

        public override void ExplicitVisit(SelectSetVariable node)
        {
            Visit(node);
            node.Expression?.Accept(this);
            // but don't visit variable - we'll do that in QuerySpecification
        }

        public override void ExplicitVisit(GoToStatement node)
        {
            AssignmentTracking = false; // give up
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(IfStatement node)
        {
            AssignmentTracking = false; // give up
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(WhileStatement node)
        {
            AssignmentTracking = false; // give up
            base.ExplicitVisit(node);
        }

        public override void Visit(DeclareTableVariableBody node)
        {
            OnDeclare(new(node.VariableName, VariableFlags.NoValue | VariableFlags.Table));
            base.Visit(node);
        }

        public override void Visit(ExecuteStatement node)
        {
            parser.Flags |= ParseFlags.MaybeQuery;
            base.Visit(node);
        }

        private void AddQuery()
        {
            switch (parser.Flags & (ParseFlags.Query | ParseFlags.Queries))
            {
                case ParseFlags.None:
                    parser.Flags |= ParseFlags.Query;
                    break;
                case ParseFlags.Query:
                    parser.Flags |= ParseFlags.Queries;
                    break;
            }
        }
        public override void Visit(SelectStatement node)
        {
            if (node.QueryExpression is QuerySpecification spec
                && spec.SelectElements is { Count: 1 }
                && spec.SelectElements[0] is SelectScalarExpression { Expression: FunctionCall func }
                && string.Equals(func.FunctionName?.Value, "SCOPE_IDENTITY", StringComparison.OrdinalIgnoreCase))
            {
                parser.OnSelectScopeIdentity(new Location(func));
            }
            AddQuery();

            base.Visit(node);
        }

        public override void Visit(OutputClause node)
        {
            AddQuery(); // works like a query
            base.Visit(node);
        }

        public override void ExplicitVisit(ExecuteSpecification node)
        {
            Visit(node);
            node.LinkedServer?.Accept(this);
            node.ExecuteContext?.Accept(this);
            if (node.ExecutableEntity is not null)
            {
                node.ExecutableEntity.Accept(this);
                if (node.ExecutableEntity is ExecutableStringList list && list.Strings.Count == 1
                    && list.Strings[0] is VariableReference)
                {
                    parser.OnExecVariable(new Location(node));
                }
            }
            if (node.Variable is not null)
            {
                MarkAssigned(node.Variable, false);
                // but don't visit
            }
            // mark any output parameters as assigned
            if (node.ExecutableEntity is not null)
            {
                foreach (var p in node.ExecutableEntity.Parameters)
                {
                    if (p.IsOutput && p.ParameterValue is VariableReference variable)
                    {
                        MarkAssigned(variable, false);
                    }
                }
            }

        }

        public override void ExplicitVisit(ExecuteParameter node)
        {
            Visit(node);
            // node.Variable?.Accept(this); // this isn't our variable; don't demand it exists
            if (node.IsOutput && node.ParameterValue is VariableReference)
            {
                // don't visit - we don't demand a value before
            }
            else
            {
                // default handling
                node.ParameterValue?.Accept(this);
            }
        }

        public override void ExplicitVisit(InsertSpecification node)
        {
            Visit(node);
            node.TopRowFilter?.Accept(this);
            node.OutputClause?.Accept(this);
            node.OutputIntoClause?.Accept(this);
            node.InsertSource?.Accept(this);
            foreach (var col in node.Columns)
            {
                col.Accept(this);
            }
            if (node.Target is VariableTableReference variable)
            {
                MarkAssigned(variable.Variable, true); // note we're effectively after insert-source, so: fine
                // but don't visit
            }
            else
            {
                node.Target?.Accept(this);
            }
        }
        public override void Visit(GlobalVariableExpression node)
        {
            if (string.Equals(node.Name, "@@IDENTITY", StringComparison.OrdinalIgnoreCase))
            {
                parser.OnGlobalIdentity(new Location(node));
            }
            base.Visit(node);
        }

        private void MarkAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, true);
        private void EnsureAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, false);
        private void EnsureOrMarkAssigned(VariableReference node, bool isTable, bool mark)
        {
            if (variables.TryGetValue(node.Name, out var existing))
            {
                var blame = existing.WithLocation(node);
                if (mark)
                {
                    if (existing.IsUnconsumed && !existing.IsTable)
                    {
                        if (AssignmentTracking) parser.OnVariableValueNotConsumed(blame);
                    }
                    // mark as has value + unconsumed
                    variables[node.Name] = existing.WithUnconsumedValue().WithLocation(node);
                }
                else
                {
                    if (existing.IsTable != isTable)
                    {
                        if (existing.IsTable)
                        {
                            parser.OnTableVariableUsedAsScalar(blame);
                        }
                        else
                        {
                            parser.OnScalarVariableUsedAsTable(blame);
                        }
                    }
                    if (existing.NoValue)
                    {
                        if (AssignmentTracking) parser.OnVariableAccessedBeforeAssignment(blame);
                    }
                    else if (existing.IsUnconsumed)
                    {
                        variables[node.Name] = existing.WithConsumed();
                    }
                }
            }
            else
            {
                var flags = VariableFlags.Parameter | (isTable ? VariableFlags.Table : VariableFlags.None);

                if (parser.TryGetParameter(node.Name, out var direction) && direction != ParameterDirection.ReturnValue)
                {
                    switch (direction)
                    {
                        case ParameterDirection.Output:
                        case ParameterDirection.InputOutput:
                            flags |= VariableFlags.OutputParameter;
                            break;
                    }

                    if (mark && !isTable) flags |= VariableFlags.Unconsumed;

                    var variable = new Variable(node, flags);
                    OnDeclare(variable);

                    if (!mark && direction == ParameterDirection.Output)
                    {
                        // pure output param, and first time we're seeing it is a read: that's not right
                        parser.OnVariableAccessedBeforeAssignment(variable);
                    }
                    else if (mark && direction is ParameterDirection.Input or ParameterDirection.InputOutput)
                    {
                        // we haven't consumed the original value - but watch out for "if" etc
                        if (AssignmentTracking) parser.OnVariableValueNotConsumed(variable);
                    }

                    if (variable.IsTable && variable.IsOutputParameter)
                    {
                        parser.OnTableVariableOutputParameter(variable);
                    }
                }
                else
                {
                    parser.OnVariableNotDeclared(new (node, flags));
                }
            }
        }

        public override void Visit(VariableReference node)
        {
            EnsureAssigned(node, false);
            base.Visit(node);
        }

        public override void ExplicitVisit(VariableTableReference node)
        {
            Visit(node);
            EnsureAssigned(node.Variable, true);
            // do *NOT* call  node.Variable?.Accept(this); - would trigger table/scalar warning
            node.Alias?.Accept(this);
        }
    }

    protected virtual bool TryGetParameter(string name, out ParameterDirection direction)
    {
        // simple mode; assume an input param exists
        direction = ParameterDirection.Input;
        return true;
    }
}

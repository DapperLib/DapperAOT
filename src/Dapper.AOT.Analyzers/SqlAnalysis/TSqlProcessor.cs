using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.IO;

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
    }
    private readonly VariableTrackingVisitor visitor;

    public TSqlProcessor(bool caseSensitive = false, Action<string>? log = null)
    {
        visitor = log is null ? new VariableTrackingVisitor(caseSensitive, this) : new LoggingVariableTrackingVisitor(caseSensitive, this, log);
    }
    public bool Execute(string sql)
    {
        Reset();
        var parser = new TSql160Parser(true, SqlEngineType.All);
        TSqlFragment tree;
        using (var reader = new StringReader(sql))
        {
            tree = parser.Parse(reader, out var errors);
            foreach (var error in errors)
            {
                OnParseError(error, new Location(error.Line, error.Column, error.Offset, 0));
            }
        }
        tree.Accept(visitor);
        return true;
    }

    public IEnumerable<Variable> Variables => visitor.Variables;

    public bool HaveReturn { get; private set; }
    public virtual void Reset()
    {
        HaveReturn = false;
        visitor.Reset();
    }

    protected virtual void OnError(string error, in Location location) { }

    protected virtual void OnParseError(ParseError error, Location location)
        => OnError($"{error.Message} (#{error.Number})", location);

    protected virtual void OnVariableAccessedBeforeDeclaration(Variable variable)
        => OnError($"Variable {variable.Name} accessed before declaration", variable.Location);

    protected virtual void OnVariableAccessedBeforeAssignment(Variable variable)
        => OnError($"Variable {variable.Name} accessed before being {(variable.IsTable ? "populated" : "assigned")}", variable.Location);

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

    protected virtual void OnNewSyntaxRecommendation(string used, string recommended, Location location, bool strong)
    {
        if (strong)
        {
            OnError($"You should use {recommended} in place of {used}", location);
        }
        else
        {
            OnError($"You may prefer to use {recommended} in place of {used}", location);
        }
    }

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
        public bool IsParameter => (Flags & VariableFlags.Parameter) != 0;

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

        public Variable WithValue() => new(in this, Flags & ~VariableFlags.NoValue);
        public Variable WithFlags(VariableFlags flags) => new(in this, flags);

        internal Variable WithLocation(VariableReference node) => new(in this, new Location(node));
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
                default:
                    log(node.GetType().Name);
                    break;
            }
            base.Visit(node);
        }
    }
    class VariableTrackingVisitor : TSqlFragmentVisitor
    {
        public VariableTrackingVisitor(bool caseSensitive, TSqlProcessor parser)
        {
            variables = caseSensitive ? new(StringComparer.Ordinal) : new(StringComparer.OrdinalIgnoreCase);
            this.parser = parser;
        }

        private readonly Dictionary<string, Variable> variables;
        private int batchCount;
        private readonly TSqlProcessor parser;

        public IEnumerable<Variable> Variables => variables.Values;

        public virtual void Reset()
        {
            variables.Clear();
            batchCount = 0;
        }

        public override void ExplicitVisit(TSqlBatch node)
        {
            if (++batchCount >= 2)
            {
                parser.OnAdditionalBatch(new Location(node));
            }
            base.ExplicitVisit(node);
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
                    parser.OnVariableAccessedBeforeDeclaration(existing);
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
            OnDeclare(new(node.VariableName, VariableFlags.NoValue));
            base.ExplicitVisit(node);
            // assign if there is a value
            if (node.Value is not null)
            {
                var name = node.VariableName.Value;
                variables[name] = variables[name].WithValue();
            }
        }

        public override void ExplicitVisit(OutputIntoClause node)
        {
            if (node.IntoTable is VariableTableReference variable)
            {
                MarkAssigned(variable.Variable, true);
            }
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ReturnStatement node)
        {
            parser.HaveReturn = true;
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(SetVariableStatement node)
        {
            base.ExplicitVisit(node.Expression);
            MarkAssigned(node.Variable, false);
        }

        public override void ExplicitVisit(BooleanComparisonExpression node)
        {
            if (node.FirstExpression is NullLiteral)
            {
                parser.OnNullLiteralComparison(new Location(node.FirstExpression));
            }
            if (node.SecondExpression is NullLiteral)
            {
                parser.OnNullLiteralComparison(new Location(node.SecondExpression));
            }
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(SelectSetVariable node)
        {
            base.ExplicitVisit(node.Expression);
            MarkAssigned(node.Variable, false);
        }

        public override void ExplicitVisit(DeclareTableVariableBody node)
        {
            OnDeclare(new(node.VariableName, VariableFlags.NoValue | VariableFlags.Table));
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ExecuteSpecification node)
        {
            if (node.ExecutableEntity is not null)
            {
                ExplicitVisit(node.ExecutableEntity);
                if (node.ExecutableEntity is ExecutableStringList list && list.Strings.Count == 1
                    && list.Strings[0] is VariableReference)
                {
                    parser.OnNewSyntaxRecommendation("EXEC with dynamic content", "EXEC sp_executesql with parameterized input",
                        new Location(node), true);
                }
            }
            if (node.ExecuteContext is not null) ExplicitVisit(node.ExecuteContext);
            if (node.LinkedServer is not null) ExplicitVisit(node.LinkedServer);
            if (node.Variable is not null)
            {
                MarkAssigned(node.Variable, false);
            }
        }

        public override void ExplicitVisit(ExecuteParameter node)
        {
            if (node.IsOutput && node.ParameterValue is VariableReference variable)
            {
                // don't demand a value before, so: just mark it assigned
                MarkAssigned(variable, false);
            }
            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(InsertSpecification node)
        {
            // we do *not* want to touch the Target
            if (node.InsertSource is not null) ExplicitVisit(node.InsertSource);
            if (node.OutputClause is not null) ExplicitVisit(node.OutputClause);
            if (node.OutputIntoClause is not null) ExplicitVisit(node.OutputIntoClause);
            if (node.Target is VariableTableReference variable)
            {
                MarkAssigned(variable.Variable, true);
            }
        }
        public override void ExplicitVisit(GlobalVariableExpression node)
        {
            var functionName = node.Name;
            if (string.Equals(functionName, "@@IDENTITY", StringComparison.OrdinalIgnoreCase))
            {
                parser.OnNewSyntaxRecommendation(functionName, "INSERT...OUTPUT or SCOPE_IDENTITY()", new Location(node), true);
            }
            base.ExplicitVisit(node);
        }
        public override void ExplicitVisit(FunctionCall node)
        {
            var functionName = node.FunctionName?.Value ?? "";
            if (string.Equals(functionName, "SCOPE_IDENTITY", StringComparison.OrdinalIgnoreCase))
            {
                parser.OnNewSyntaxRecommendation(functionName + "()", "INSERT...OUTPUT", new Location(node), false);
            }
            base.ExplicitVisit(node);
        }

        private void MarkAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, true);
        private void EnsureAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, false);
        private void EnsureOrMarkAssigned(VariableReference node, bool isTable, bool mark)
        {
            if (variables.TryGetValue(node.Name, out var existing))
            {
                if (mark)
                {
                    if (existing.NoValue)
                    {
                        variables[node.Name] = existing.WithValue();
                    }
                }
                else
                {
                    var blame = existing.WithLocation(node);
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
                        parser.OnVariableAccessedBeforeAssignment(blame);
                    }
                }
            }
            else
            {
                // assume it is a parameter with a value (we'll catch any later definition separately)
                OnDeclare(new(node, VariableFlags.Parameter | (isTable ? VariableFlags.Table : VariableFlags.None)));
            }
        }

        public override void ExplicitVisit(VariableReference node)
        {
            EnsureAssigned(node, false);
        }

        public override void ExplicitVisit(VariableTableReference node)
        {
            EnsureAssigned(node.Variable, true);
        }
    }
}

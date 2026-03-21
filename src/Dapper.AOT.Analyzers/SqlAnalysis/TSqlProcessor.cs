using Dapper.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        Declared = 1 << 5,
        UsedInQuery = 1 << 6,
    }
    private readonly VariableTrackingVisitor _visitor;

    protected bool CaseSensitive => _visitor.CaseSensitive;
    protected bool ValidateSelectNames => _visitor.ValidateSelectNames;
    public TSqlProcessor(SqlParseInputFlags flags = SqlParseInputFlags.None, Action<string>? log = null)
    {
        _visitor = log is null ? new VariableTrackingVisitor(flags, this) : new LoggingVariableTrackingVisitor(flags, this, log);
    }

    static string ReplaceDapperSyntaxWithValidSql(string sql, ImmutableArray<SqlParameter> members)
    {
        if (SqlTools.LiteralTokens.IsMatch(sql))
        {
            // some padding here to get the same size, for location data; "{=foo}" to " @foo "
            sql = SqlTools.LiteralTokens.Replace(sql, static match => " @" + match.Groups[1].Value + " ");
        }

        if (!members.IsDefaultOrEmpty)
        {
            foreach (var member in members)
            {
                if (member.IsExpandable)
                {
                    var regexIncludingUnknown = ("(?<![?@:$\\p{L}\\p{N}_])([?@:$]" + Regex.Escape(member.Name) + @")(?!\w)(\s+(?i)unknown(?-i))?");
                    sql = Regex.Replace(sql, regexIncludingUnknown, match =>
                    {
                        var variableName = match.Groups[1].Value;
                        if (match.Groups[2].Success)
                        {
                            // looks like an optimize hint; leave it alone it
                            return match.Value;
                        }
                        else
                        {
                            // expand it (as one for now)
                            return "(@" + member.Name + ")";
                        }
                    }, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
                }
            }
        }

        return sql;
    }

    public virtual bool Execute(string sql, ImmutableArray<SqlParameter> members = default)
    {
        if (members.IsDefault)
        {
            members = ImmutableArray<SqlParameter>.Empty;
        }
        Reset();
        _visitor.AddParameters(members);
        var fixedSql = ReplaceDapperSyntaxWithValidSql(sql, members);
        if (fixedSql != sql)
        {
            Flags |= SqlParseOutputFlags.SqlAdjustedForDapperSyntax;
        }
        
        var sqlProcessingContext = new SqlProcessingContext(_visitor, fixedSql);

        var memberNamesSet = new HashSet<string>(members.Select(x => x.Name), CaseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase);
        sqlProcessingContext.MarkPseudoPositionalVariablesUsed(memberNamesSet);
        
        var parser = new TSql160Parser(true, SqlEngineType.All);
        TSqlFragment tree;
        using (var reader = new StringReader(fixedSql))
        {
            tree = parser.Parse(reader, out var errors);
            if (errors is not null && errors.Count != 0)
            {
                foreach (var error in sqlProcessingContext.GetErrorsToReport(errors))
                {
                    Flags |= SqlParseOutputFlags.SyntaxError;
                    OnParseError(error, new Location(error.Line, error.Column, error.Offset, 0));
                }
            }
            else
            {
                Flags |= SqlParseOutputFlags.Reliable;
            }
        }

        tree.Accept(_visitor);

        // check state of locals after proc - was anything written and not read?
        foreach (var variable in _visitor.Variables)
        {
            if (variable.IsUnusedParameter)
            {
                OnUnusedParameter(variable);
            }
            else if (!variable.IsDeclared)
            {
                if (_visitor.KnownParameters) OnVariableNotDeclared(variable);
            }
            else if (variable is { IsUnconsumed: true, IsTable: false, IsOutputParameter: false })
            {
                if (_visitor.AssignmentTracking) OnVariableValueNotConsumed(variable);
            }

        }

        return true;
    }

    public IEnumerable<Variable> Variables => _visitor.Variables;

    public SqlParseOutputFlags Flags { get; private set; }
    public virtual void Reset()
    {
        var flags = SqlParseOutputFlags.None;
        if (_visitor.KnownParameters) flags |= SqlParseOutputFlags.KnownParameters;
        Flags = flags;

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

    protected virtual void OnUnusedParameter(Variable variable)
        => OnError($"Parameter {variable.Name} is not not used in the query", variable.Location);

    protected virtual void OnDuplicateVariableDeclaration(Variable variable)
        => OnError($"Variable {variable.Name} is declared multiple times", variable.Location);

    protected virtual void OnVariableParameterConflict(Variable variable)
        => OnError($"Variable {variable.Name} conflicts with a parameter of the same name", variable.Location);

    protected virtual void OnScalarVariableUsedAsTable(Variable variable)
        => OnError($"Scalar variable {variable.Name} is used like a table", variable.Location);

    protected virtual void OnTableVariableUsedAsScalar(Variable variable)
        => OnError($"Table variable {variable.Name} is used like a scalar", variable.Location);

    protected virtual void OnNullLiteralComparison(Location location)
        => OnError("Null literals should not be used in binary comparisons; prefer 'is null' and 'is not null'", location);

    protected virtual void OnPseudoPositionalParameter(Location location)
        => OnError("It is more like Dapper will incorrectly treat this literal as a pseudo-positional parameter", location);

    private void OnSimplifyExpression(Location location, int? value)
        => OnSimplifyExpression(location, value is null ? "null" : value.Value.ToString(CultureInfo.InvariantCulture));
    private void OnSimplifyExpression(Location location, decimal? value)
        => OnSimplifyExpression(location, value switch
        {
            null => "null",
            0M => "0.0",
            _ => value.Value.ToString(CultureInfo.InvariantCulture),
        });

    protected virtual void OnDivideByZero(Location location)
        => OnError("Division by zero detected", location);
    protected virtual void OnTrivialOperand(Location location)
        => OnError("Operand makes this calculation trivial; it can be simplified", location);
    protected virtual void OnInvalidNullExpression(Location location)
        => OnError("Operation requires non-null operand", location);

    protected virtual void OnSimplifyExpression(Location location, string value)
        => OnError($"Expression can be simplified to '{value}'", location);

    protected virtual void OnAdditionalBatch(Location location)
        => OnError("Multiple batches are not permitted", location);

    protected virtual void OnGlobalIdentity(Location location)
        => OnError("@@identity should not be used; use SCOPE_IDENTITY() instead", location);

    protected virtual void OnSelectScopeIdentity(Location location)
        => OnError("Consider OUTPUT INSERTED.yourid on the INSERT instead of SELECT SCOPE_IDENTITY()", location);

    protected virtual void OnExecComposedSql(Location location)
        => OnError("EXEC with composed SQL may be susceptible to SQL injection; consider EXEC sp_executesql with parameters", location);

    protected virtual void OnTableVariableOutputParameter(Variable variable)
        => OnError($"Table variable {variable.Name} cannot be used as an output parameter", variable.Location);

    protected virtual void OnInsertColumnsNotSpecified(Location location)
        => OnError("Target columns for INSERT should be explicitly specified", location);

    protected virtual void OnInsertColumnMismatch(Location location)
        => OnError("The columns specified in the INSERT do not match the source columns provided", location);
    protected virtual void OnInsertColumnsUnbalanced(Location location)
        => OnError("The rows specified in the INSERT have differing widths", location);

    protected virtual void OnSelectStar(Location location)
        => OnError("SELECT columns should be explicitly specified", location);
    protected virtual void OnSelectEmptyColumnName(Location location, int column)
        => OnError($"SELECT statement with missing column name: {column}", location);
    protected virtual void OnSelectDuplicateColumnName(Location location, string name)
        => OnError($"SELECT statement with duplicate column name: {name}", location);
    protected virtual void OnSelectAssignAndRead(Location location)
        => OnError("SELECT statement has both assignment and results", location);

    protected virtual void OnDeleteWithoutWhere(Location location)
        => OnError("DELETE statement without WHERE clause", location);
    protected virtual void OnUpdateWithoutWhere(Location location)
        => OnError("UPDATE statement without WHERE clause", location);

    protected virtual void OnSelectSingleTopError(Location location)
        => OnError("SELECT for Single* should use TOP 2; if you do not need to test over-read, use First*", location);
    protected virtual void OnSelectFirstTopError(Location location)
        => OnError("SELECT for First* should use TOP 1", location);
    protected virtual void OnSelectSingleRowWithoutWhere(Location location)
        => OnError("SELECT for single row without WHERE or (TOP and ORDER BY)", location);
    protected virtual void OnSelectAggregateAndNonAggregate(Location location)
        => OnError("SELECT has mixture of aggregate and non-aggregate expressions", location);
    protected virtual void OnNonPositiveTop(Location location)
        => OnError("TOP literals should be positive", location);
    protected virtual void OnNonPositiveFetch(Location location)
        => OnError("FETCH literals should be positive", location);
    protected virtual void OnNegativeOffset(Location location)
        => OnError("OFFSET literals should be non-negative", location);
    protected virtual void OnNonIntegerTop(Location location)
        => OnError("TOP literals should be integers", location);
    protected virtual void OnFromMultiTableMissingAlias(Location location)
        => OnError("FROM expressions with multiple elements should use aliases", location);
    protected virtual void OnFromMultiTableUnqualifiedColumn(Location location, string name)
        => OnError($"FROM expressions with multiple elements should qualify all columns; it is unclear where '{name}' is located", location);

    protected virtual void OnInvalidDatepartToken(Location location)
        => OnError("Valid datepart token expected", location);
    protected virtual void OnTopWithOffset(Location location)
        => OnError("TOP cannot be used when OFFSET is specified", location);


    internal readonly struct Location
    {
        public static implicit operator Location(TSqlFragment source) => new(source);
        private Location(TSqlFragment source) : this()
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

        public static readonly Location Zero = new(0, 0, 0, 0);

        public readonly int Line, Column, Offset, Length;

        public bool IsZero => Length == 0 && Offset == 0 && Line == 0 && Column == 0;

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
        public bool IsDeclared => (Flags & VariableFlags.Declared) != 0;
        public bool IsUnused => (Flags & VariableFlags.UsedInQuery) == 0;
        public bool IsUnusedParameter => (Flags & (VariableFlags.Parameter | VariableFlags.Declared | VariableFlags.UsedInQuery)) == (VariableFlags.Parameter | VariableFlags.Declared);

        public Variable(SqlParameter parameter)
        {
            Name = parameter.Name;
            Location = Location.Zero;
            Flags = VariableFlags.Parameter | VariableFlags.Declared;
            switch (parameter.Direction)
            {
                case ParameterDirection.Input:
                    Flags |= VariableFlags.Unconsumed;
                    break;
                case ParameterDirection.Output:
                    Flags |= VariableFlags.NoValue | VariableFlags.OutputParameter;
                    break;
                case ParameterDirection.InputOutput:
                    Flags |= VariableFlags.Unconsumed | VariableFlags.OutputParameter;
                    break;
            }
            if (parameter.IsTable) Flags |= VariableFlags.Table;
        }

        public Variable(Identifier identifier, VariableFlags flags)
        {
            Flags = flags;
            Name = identifier.Value;
            Location = identifier;
        }
        public Variable(VariableReference reference, VariableFlags flags)
        {
            Flags = flags;
            Name = reference.Name;
            Location = reference;
        }

        private Variable(scoped in Variable source, VariableFlags flags)
        {
            this = source;
            Flags = flags;
        }

        private Variable(scoped in Variable source, string name)
        {
            this = source;
            Name = name;
        }

        private Variable(scoped in Variable source, Location location)
        {
            this = source;
            Location = location;
        }

        public Variable WithConsumed() => new(in this, (Flags & ~VariableFlags.Unconsumed));
        public Variable WithDeclared() => new(in this, (Flags | VariableFlags.Declared));
        public Variable WithUsed() => new(in this, (Flags | VariableFlags.UsedInQuery));
        public Variable WithUnconsumedValue() => new(in this, (Flags & ~VariableFlags.NoValue) | VariableFlags.Unconsumed);
        public Variable WithFlags(VariableFlags flags) => new(in this, flags);

        public Variable WithLocation(TSqlFragment node) => new(in this, node);
        public Variable WithName(string name) => new(in this, name);
    }
    
    internal class LoggingVariableTrackingVisitor : VariableTrackingVisitor
    {
        private readonly Action<string> log;
        public LoggingVariableTrackingVisitor(SqlParseInputFlags flags, TSqlProcessor parser, Action<string> log) : base(flags, parser)
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
    
    internal class VariableTrackingVisitor : TSqlFragmentVisitor
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

        private readonly SqlParseInputFlags _flags;
        public VariableTrackingVisitor(SqlParseInputFlags flags, TSqlProcessor parser)
        {
            _flags = flags;
            _variables = CaseSensitive ? new(StringComparer.Ordinal) : new(StringComparer.OrdinalIgnoreCase);
            this.parser = parser;
        }

        public bool CaseSensitive => (_flags & SqlParseInputFlags.CaseSensitive) != 0;
        public bool ValidateSelectNames => (_flags & SqlParseInputFlags.ValidateSelectNames) != 0;
        public bool SingleRow => (_flags & SqlParseInputFlags.SingleRow) != 0;
        public bool AtMostOne => (_flags & SqlParseInputFlags.AtMostOne) != 0;
        public bool KnownParameters => (_flags & SqlParseInputFlags.KnownParameters) != 0;

        private bool TryGetVariable(string name, out Variable variable)
            => _variables.TryGetValue(name, out variable) || SlowTryGetVariable(name, out variable);

        private bool SlowTryGetVariable(string name, out Variable variable)
        {
            // parameters may have been added as "foo", but resolved by the parser as "@foo", ":foo", etc
            // so: let's fix that!
            string? nameWithoutPrefix = null;
            if (name is not null && name.Length >= 2) // at least "@a"
            {
                foreach (var pair in _variables)
                {
                    if (pair.Value.IsParameter && pair.Key.Length == name.Length - 1 // key "foo", name "@foo"
                        && SqlTools.ParameterPrefixCharacters.IndexOf(name[0]) >= 0 // name starts with token
                        && SqlTools.ParameterPrefixCharacters.IndexOf(pair.Key[0]) < 0) // key doesn't
                    {
                        nameWithoutPrefix ??= name.Substring(1);
                        // if same: remove the old and add the fixed-up one
                        if (_variables.Comparer.Equals(nameWithoutPrefix, pair.Key) && _variables.Remove(pair.Key))
                        {
                            variable = pair.Value.WithName(name);
                            _variables.Add(name, variable);
                            return true;
                        }
                    }
                }
            }
            variable = default;
            return false;
        }

        internal Variable SetVariable(Variable variable) => _variables[variable.Name] = variable;

        internal bool TryGetByName(string name, out Variable variable)
        {
            if (_variables.TryGetValue(name, out var value))
            {
                variable = value;
                return true;
            }

            variable = default;
            return false;
        }

        private readonly Dictionary<string, Variable> _variables;
        private int batchCount;
        private readonly TSqlProcessor parser;
        public bool AssignmentTracking { get; private set; }

        public IEnumerable<Variable> Variables => _variables.Values;

        public void AddParameters(ImmutableArray<SqlParameter> parameters)
        {
            if (!parameters.IsDefaultOrEmpty)
            {
                foreach (var arg in parameters)
                {
                    var parsed = SetVariable(new(arg));
                    if (parsed.IsOutputParameter && parsed.IsTable)
                    {
                        parser.OnTableVariableOutputParameter(parsed);
                    }
                }
            }
        }
        public virtual void Reset()
        {
            AssignmentTracking = true;
            _variables.Clear();
            batchCount = 0;
        }

        public override void Visit(TSqlBatch node)
        {
            if (++batchCount >= 2)
            {
                parser.OnAdditionalBatch(node);
            }
            base.Visit(node);
        }

        private void OnDeclare(Variable variable)
        {
            if (TryGetVariable(variable.Name, out var existing))
            {
                if (existing.IsDeclared)
                {
                    // two definitions? yeah, that's wrong
                    if (existing.IsParameter)
                    {
                        parser.OnVariableParameterConflict(variable);
                    }
                    else
                    {
                        parser.OnDuplicateVariableDeclaration(variable);
                    }
                }
                else
                {
                    // we previously assumed it was a parameter, but actually it was accessed before declaration
                    existing = SetVariable(existing.WithFlags(variable.Flags | VariableFlags.Declared));
                    // but the *original* one was accessed invalidly
                    if (AssignmentTracking) parser.OnVariableAccessedBeforeDeclaration(existing);
                }
            }
            else
            {
                SetVariable(variable.WithDeclared());
            }
        }

        public override void ExplicitVisit(DeclareVariableElement node)
        {
            Visit(node);
            if (node.VariableName is not null)
            {
                OnDeclare(new(node.VariableName, VariableFlags.NoValue));
                node.VariableName.Accept(this);
            }
            node.DataType?.Accept(this);
            node.Nullable?.Accept(this);
            // assign if there is a value
            if (node.Value is not null)
            {
                node.Value.Accept(this);
                // mark assigned
                if (node.VariableName is not null && TryGetVariable(node.VariableName.Value, out var variable))
                {
                    SetVariable(variable.WithUnconsumedValue());
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
            parser.Flags |= SqlParseOutputFlags.Return;
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
                parser.OnNullLiteralComparison(node.FirstExpression);
            }
            if (node.SecondExpression is NullLiteral)
            {
                parser.OnNullLiteralComparison(node.SecondExpression);
            }
            base.Visit(node);
        }

        public override void Visit(WhereClause whereClause)
        {
            base.Visit(whereClause);
        }

        public override void Visit(StringLiteral node)
        {
            foreach (var token in node.ScriptTokenStream)
            {
                if (token.TokenType is not TSqlTokenType.AsciiStringLiteral)
                {
                    continue;
                }

                int firstAppearance;
                if ((firstAppearance = token.Text.IndexOf('?')) >= 0
                    && token.Text.IndexOf('?', firstAppearance + 1) >= 0 // if there are 2 appearances of `?`
                    && CompiledRegex.PseudoPositional.IsMatch(node.Value))
                {
                    parser.OnPseudoPositionalParameter(node);
                }
            }

            base.Visit(node);
        }

        public override void ExplicitVisit(QuerySpecification node)
        {
            var oldDemandAlias = _demandAliases;
            try
            {
                // set ambient state so we can complain more as we walk the nodes
                _demandAliases = IsMultiTable(node.FromClause)
                    ? new(true, null) : default;
                base.ExplicitVisit(node);
            }
            finally
            {
                _demandAliases = oldDemandAlias;
            }
            foreach (var el in node.SelectElements)
            {
                if (el is SelectSetVariable setVar && setVar.Variable is not null)
                {
                    // assign *after* the select
                    MarkAssigned(setVar.Variable, false);
                }
            }
        }

        public override void Visit(QueryExpression expression)
        {
            base.Visit(expression);
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
            parser.Flags |= SqlParseOutputFlags.MaybeQuery;
            base.Visit(node);
        }

        private bool AddQuery() // returns true if the first
        {
            switch (parser.Flags & (SqlParseOutputFlags.Query | SqlParseOutputFlags.Queries))
            {
                case SqlParseOutputFlags.None:
                    parser.Flags |= SqlParseOutputFlags.Query;
                    return true;
                case SqlParseOutputFlags.Query:
                    parser.Flags |= SqlParseOutputFlags.Queries;
                    break;
            }
            return false;
        }

        public override void Visit(QuerySpecification node)
        {
            if (node.TopRowFilter is not null && node.OffsetClause is not null)
            {
                parser.OnTopWithOffset(node.TopRowFilter);
            }
            base.Visit(node);
        }

        public override void ExplicitVisit(FunctionCall node)
        {
            ScalarExpression? ignore = null;
            if (node.Parameters.Count != 0 && IsSpecialDateFunction(node.FunctionName.Value))
            {
                ValidateDateArg(ignore = node.Parameters[0]);
            }
            var oldIgnore = _demandAliases.IgnoreNode; // stash
            _demandAliases.IgnoreNode = ignore;
            base.ExplicitVisit(node); // dive
            _demandAliases.IgnoreNode = oldIgnore; // restore

            static bool IsSpecialDateFunction(string name)
                => name.StartsWith("DATE", StringComparison.OrdinalIgnoreCase)
                && IsAnyCaseInsensitive(name, DateTokenFunctions);
        }

        static bool IsAnyCaseInsensitive(string value, string[] options)
        {
            if (value is not null)
            {
                foreach (var option in options)
                {
                    if (string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // arg0 has special meaning - not a column/etc
        static readonly string[] DateTokenFunctions = ["DATE_BUCKET", "DATEADD", "DATEDIFF", "DATEDIFF_BIG", "DATENAME", "DATEPART", "DATETRUNC"];
        static readonly string[] DateTokens = [
            "year", "yy", "yyyy",
            "quarter", "qq", "q",
            "month", "mm", "m",
            "dayofyear", "dy", "y",
            "day", "dd", "d",
            "week", "wk", "ww",
            "weekday", "dw", "w",
            "hour", "hh",
            "minute", "mi", "n",
            "second", "ss", "s",
            "millisecond", "ms",
            "microsecond", "mcs",
            "nanosecond", "ns"
            ];

        private void ValidateDateArg(ScalarExpression value)
        {
            if (!(value is ColumnReferenceExpression col
                && col.MultiPartIdentifier.Count == 1 && IsAnyCaseInsensitive(
                    col.MultiPartIdentifier[0].Value, DateTokens)))
            {
                parser.OnInvalidDatepartToken(value);
            }

        }

        public override void Visit(SelectStatement node)
        {
            if (node.QueryExpression is QuerySpecification spec)
            {

                if (spec.SelectElements is { Count: 1 }
                    && spec.SelectElements[0] is SelectScalarExpression { Expression: FunctionCall func }
                    && string.Equals(func.FunctionName?.Value, "SCOPE_IDENTITY", StringComparison.OrdinalIgnoreCase))
                {
                    parser.OnSelectScopeIdentity(func);
                }
                int sets = 0, reads = 0;
                var checkNames = ValidateSelectNames;
                HashSet<string> names = checkNames ? new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) : null!;

                var aggregate = AggregateFlags.None;
                int index = 0;
                foreach (var el in spec.SelectElements)
                {
                    switch (el)
                    {
                        case SelectStarExpression:
                            parser.OnSelectStar(el);
                            aggregate |= AggregateFlags.HaveNonAggregate;
                            reads++;
                            break;
                        case SelectScalarExpression scalar:
                            if (checkNames)
                            {
                                var name = scalar.ColumnName?.Value;
                                if (name is null && scalar.Expression is ColumnReferenceExpression col)
                                {
                                    var ids = col.MultiPartIdentifier.Identifiers;
                                    if (ids.Count != 0)
                                    {
                                        name = ids[ids.Count - 1].Value;
                                    }
                                }
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    parser.OnSelectEmptyColumnName(scalar, index);
                                }
                                else if (!names.Add(name!))
                                {
                                    parser.OnSelectDuplicateColumnName(scalar, name!);
                                }
                            }
                            aggregate |= IsAggregate(scalar.Expression);
                            reads++;
                            break;
                        case SelectSetVariable:
                            sets++;
                            break;
                    }
                    index++;
                }

                if (aggregate == (AggregateFlags.HaveAggregate | AggregateFlags.HaveNonAggregate))
                {
                    parser.OnSelectAggregateAndNonAggregate(spec);
                }

                if (reads != 0)
                {
                    if (sets != 0)
                    {
                        parser.OnSelectAssignAndRead(spec);
                    }
                    if (node.Into is null) // otherwise not actually a query
                    {
                        bool firstQuery = AddQuery();
                        if (firstQuery && SingleRow // optionally enforce single-row validation
                            && spec.FromClause is not null) // no "from" is things like 'select @id, @name' - always one row
                        {
                            bool haveTopOrFetch = false;
                            if (spec.TopRowFilter is { Percent: false, Expression: ScalarExpression top })
                            {
                                haveTopOrFetch = EnforceTop(top);
                            }
                            else if (spec.OffsetClause is { FetchExpression: ScalarExpression fetch })
                            {
                                haveTopOrFetch = EnforceTop(fetch);
                            }

                            // we want *either* a WHERE (which we will allow with/without a TOP),
                            // or a TOP + ORDER BY
                            if (!IsUnfiltered(spec.FromClause, spec.WhereClause)) { } // fine
                            else if (haveTopOrFetch && spec.OrderByClause is not null) { } // fine
                            else if ((aggregate & (AggregateFlags.HaveAggregate | AggregateFlags.Uncertain)) != 0) { } // fine
                            else
                            {
                                parser.OnSelectSingleRowWithoutWhere(node);
                            }
                        }
                    }
                }
            }

            base.Visit(node);
        }

        enum AggregateFlags
        {
            None = 0,
            HaveAggregate = 1 << 0,
            HaveNonAggregate = 1 << 1,
            Uncertain = 1 << 2,
        }

        private AggregateFlags IsAggregate(ScalarExpression expression)
        {
            // any use of an aggregate function contributes HaveAggregate
            // - there could be unary/binary operations on that aggregate function
            // - column references etc inside an aggregate expression,
            //   otherwise they contribute HaveNonAggregate
            switch (expression)
            {
                case Literal:
                    return AggregateFlags.None;
                case UnaryExpression ue:
                    return IsAggregate(ue.Expression);
                case BinaryExpression be:
                    return IsAggregate(be.FirstExpression)
                        | IsAggregate(be.SecondExpression);
                case FunctionCall func when IsAggregateFunction(func.FunctionName.Value):
                    return AggregateFlags.HaveAggregate; // don't need to look inside
                case ScalarSubquery sq:
                    throw new NotSupportedException();
                case IdentityFunctionCall:
                case PrimaryExpression:
                    return AggregateFlags.HaveNonAggregate;
                default:
                    return AggregateFlags.Uncertain;
            }

            static bool IsAggregateFunction(string name)
                => IsAnyCaseInsensitive(name, AggregateFunctions);
        }

        static readonly string[] AggregateFunctions = [
            "APPROX_COUNT_DISTINCT", "AVG","CHECKSUM_AGG", "COUNT", "COUNT_BIG",
            "GROUPING", "GROUPING_ID", "MAX", "MIN", "STDEV",
            "STDEVP", "STRING_AGG", "SUM", "VAR", "VARP",
            ];

        private bool EnforceTop(ScalarExpression expr)
        {
            if (IsInt32(expr, out var i) == TryEvaluateResult.SuccessConstant && i.HasValue)
            {
                if (AtMostOne) // Single[OrDefault][Async]
                {
                    if (i.Value != 2)
                    {
                        parser.OnSelectSingleTopError(expr);
                    }
                }
                else // First[OrDefault][Async]
                {
                    if (i.Value != 1)
                    {
                        parser.OnSelectFirstTopError(expr);
                    }
                }
                return true;
            }
            return false;
        }

        public override void Visit(OffsetClause node)
        {
            if (IsInt32(node.FetchExpression, out var i) == TryEvaluateResult.SuccessConstant && i <= 0)
            {
                parser.OnNonPositiveFetch(node.FetchExpression);
            }
            if (IsInt32(node.OffsetExpression, out i) == TryEvaluateResult.SuccessConstant && i < 0)
            {
                parser.OnNegativeOffset(node.OffsetExpression);
            }
            base.Visit(node);
        }

        TryEvaluateResult IsInt32(ScalarExpression scalar, out int? value, int complexity = 10, bool vocal = false)
            => IsInt32Impl(scalar, out value, vocal, ref complexity);

        static bool IsHardNull(ScalarExpression expression)
        {
            while (true)
            {
                if (expression is NullLiteral) return true;
                if (expression is UnaryExpression unary)
                {
                    expression = unary.Expression; // +~~null is still hard null for the purposes of >> etc
                    continue;
                }
                return false;
            }
        }
        TryEvaluateResult IsInt32Impl(ScalarExpression scalar, out int? value, bool vocal, ref int complexity)
        {
            bool hasWarnings = false;
            try
            {
                while (scalar is ParenthesisExpression parens && complexity-- > 0)
                {
                    scalar = parens.Expression;
                }
                complexity--;
                checked
                {
                    switch (scalar)
                    {
                        case NullLiteral:
                            value = null;
                            return TryEvaluateResult.SuccessConstant;
                        case IntegerLiteral integer when int.TryParse(integer.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i):
                            value = i;
                            return TryEvaluateResult.SuccessConstant;
                        case UnaryExpression unary when complexity > 0 && IsInt32Impl(unary.Expression, out value, false, ref complexity) == TryEvaluateResult.SuccessConstant:
                            switch (unary.UnaryExpressionType)
                            {
                                case UnaryExpressionType.Negative:
                                    value = -value;
                                    return TryEvaluateResult.SuccessConstant;
                                case UnaryExpressionType.Positive:
                                    return TryEvaluateResult.SuccessConstant;
                                case UnaryExpressionType.BitwiseNot:
                                    value = ~value;
                                    return TryEvaluateResult.SuccessConstant;
                            }
                            break;
                        case BinaryExpression binary when complexity > 2:
                            var haveFirst = IsInt32Impl(binary.FirstExpression, out var first, false, ref complexity) == TryEvaluateResult.SuccessConstant;
                            var haveSecond = IsInt32Impl(binary.SecondExpression, out var second, false, ref complexity) == TryEvaluateResult.SuccessConstant;

                            // if either half is *known* to be null; we're good
                            if ((haveFirst && first is null) || (haveSecond && second is null))
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                    case BinaryExpressionType.Subtract:
                                    case BinaryExpressionType.Divide:
                                    case BinaryExpressionType.Multiply:
                                    case BinaryExpressionType.Modulo:
                                        value = null;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.BitwiseXor:
                                    case BinaryExpressionType.BitwiseOr:
                                    case BinaryExpressionType.BitwiseAnd:
                                        value = null;
                                        // need at least one not-null-literal operand
                                        if (IsHardNull(binary.FirstExpression) && IsHardNull(binary.SecondExpression))
                                        {
                                            if (vocal) parser.OnInvalidNullExpression(binary);
                                            return TryEvaluateResult.FailureNonConstantWithWarnings;
                                        }
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.LeftShift:
                                    case BinaryExpressionType.RightShift:
                                        value = null;
                                        // need both to be not-null-literal operands
                                        if (IsHardNull(binary.FirstExpression))
                                        {
                                            if (vocal) parser.OnInvalidNullExpression(binary.FirstExpression);
                                            return TryEvaluateResult.FailureNonConstantWithWarnings;
                                        }
                                        if (IsHardNull(binary.SecondExpression))
                                        {
                                            if (vocal) parser.OnInvalidNullExpression(binary.SecondExpression);
                                            return TryEvaluateResult.FailureNonConstantWithWarnings;
                                        }
                                        return TryEvaluateResult.SuccessConstant;
                                }
                                break;
                            }

                            // if we have both, we may be able to fully evaluate
                            if (haveFirst && haveSecond)
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                        value = first + second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Subtract:
                                        value = first - second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Divide when second == 0:
                                        if (vocal) parser.OnDivideByZero(binary.SecondExpression);
                                        value = null;
                                        return TryEvaluateResult.FailureNonConstantWithWarnings;
                                    case BinaryExpressionType.Divide:
                                        value = first / second; // TSQL is integer division
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Multiply:
                                        value = first * second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Modulo when second == 0:
                                        if (vocal) parser.OnDivideByZero(binary.SecondExpression);
                                        value = null;
                                        return TryEvaluateResult.FailureNonConstantWithWarnings;
                                    case BinaryExpressionType.Modulo:
                                        value = first % second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.BitwiseXor:
                                        value = first ^ second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.BitwiseOr:
                                        value = first | second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.BitwiseAnd:
                                        value = first & second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.LeftShift:
                                        if (first is null || second is null) break; // null not allowed here
                                        if (second < 0)
                                        {
                                            second = -second; // TSQL: neg-shift allowed
                                            goto case BinaryExpressionType.RightShift;
                                        }
                                        if (second >= 32) // c# shift masks "by" to 5 bits 
                                        {
                                            value = 0;
                                            return TryEvaluateResult.SuccessConstant;
                                        }
                                        value = first << second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.RightShift:
                                        if (first is null || second is null) break; // null not allowed here
                                        if (second < 0)
                                        {
                                            second = -second; // TSQL: neg-shift allowed
                                            goto case BinaryExpressionType.LeftShift;
                                        }
                                        if (second >= 32) // c# shift masks "by" to 5 bits 
                                        {
                                            value = 0;
                                            return TryEvaluateResult.SuccessConstant;
                                        }
                                        value = first >>> second; // TSQL right-shift is unsigned
                                        return TryEvaluateResult.SuccessConstant;
                                }
                            }

                            // if we have either, we may be able to check undesirable cases
                            if (vocal && (haveFirst || haveSecond))
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                    case BinaryExpressionType.Subtract:
                                        if (haveFirst && first == 0)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.Multiply:
                                        if (haveFirst && first is 0 or 1)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second is 0 or 1)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.Divide:
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnDivideByZero(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        else
                                        {
                                            if (haveFirst && first == 0)
                                            {
                                                parser.OnTrivialOperand(binary.FirstExpression);
                                                hasWarnings = true;
                                            }
                                            if (haveSecond && second == 1)
                                            {
                                                parser.OnTrivialOperand(binary.SecondExpression);
                                                hasWarnings = true;
                                            }
                                        }
                                        break;
                                    case BinaryExpressionType.Modulo:
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnDivideByZero(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        else if (haveFirst && first == 0)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.LeftShift:
                                    case BinaryExpressionType.RightShift:
                                        if (haveFirst && first == 0)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.BitwiseAnd:
                                    case BinaryExpressionType.BitwiseOr:
                                    case BinaryExpressionType.BitwiseXor:
                                        if (haveFirst && first is 0 or -1)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second is 0 or -1)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch { } // overflow etc
            value = default;
            return hasWarnings ? TryEvaluateResult.FailureNonConstantWithWarnings : TryEvaluateResult.FailureNonConstant;
        }

        enum TryEvaluateResult
        {
            FailureNonConstant = 0,
            SuccessConstant = 1,
            FailureNonConstantWithWarnings = 2,
        }
        TryEvaluateResult IsDecimal(ScalarExpression scalar, out decimal? value, int complexity = 64, bool vocal = false)
            => IsDecimalImpl(scalar, out value, vocal, ref complexity);

        TryEvaluateResult IsDecimalImpl(ScalarExpression scalar, out decimal? value, bool vocal, ref int complexity)
        {
            bool hasWarnings = false;
            try
            {
                while (scalar is ParenthesisExpression parens && complexity-- > 0)
                {
                    scalar = parens.Expression;
                }
                complexity--;
                checked
                {
                    switch (scalar)
                    {
                        case NullLiteral:
                            value = null;
                            return TryEvaluateResult.SuccessConstant;
                        case IntegerLiteral integer when int.TryParse(integer.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i):
                            value = i;
                            return TryEvaluateResult.SuccessConstant;
                        case NumericLiteral number when decimal.TryParse(number.Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var d):
                            value = d;
                            return TryEvaluateResult.SuccessConstant;
                        case UnaryExpression unary when complexity > 0 && IsDecimalImpl(unary.Expression, out value, false, ref complexity) == TryEvaluateResult.SuccessConstant:
                            switch (unary.UnaryExpressionType)
                            {
                                case UnaryExpressionType.Negative: // note -ve is always allowed, even when not complex
                                    value = -value;
                                    return TryEvaluateResult.SuccessConstant;
                                case UnaryExpressionType.Positive:
                                    return TryEvaluateResult.SuccessConstant;
                            }
                            break;
                        case BinaryExpression binary when complexity > 1:
                            var haveFirst = IsDecimalImpl(binary.FirstExpression, out var first, false, ref complexity) == TryEvaluateResult.SuccessConstant;
                            var haveSecond = IsDecimalImpl(binary.SecondExpression, out var second, false, ref complexity) == TryEvaluateResult.SuccessConstant;

                            // if either half is *known* to be null; we're good
                            if ((haveFirst && first is null) || (haveSecond && second is null))
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                    case BinaryExpressionType.Subtract:
                                    case BinaryExpressionType.Divide:
                                    case BinaryExpressionType.Multiply:
                                    case BinaryExpressionType.Modulo:
                                        value = null;
                                        return TryEvaluateResult.SuccessConstant;
                                }
                                break;
                            }

                            // if we have both, we may be able to fully evaluate
                            if (haveFirst && haveSecond)
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                        value = first + second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Subtract:
                                        value = first - second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Divide when second == 0:
                                        if (vocal) parser.OnDivideByZero(binary.SecondExpression);
                                        value = null;
                                        return TryEvaluateResult.FailureNonConstantWithWarnings;
                                    case BinaryExpressionType.Divide:
                                        value = first / second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Multiply:
                                        value = first * second;
                                        return TryEvaluateResult.SuccessConstant;
                                    case BinaryExpressionType.Modulo when second == 0:
                                        if (vocal) parser.OnDivideByZero(binary.SecondExpression);
                                        value = null;
                                        return TryEvaluateResult.FailureNonConstantWithWarnings;
                                    case BinaryExpressionType.Modulo:
                                        value = first % second;
                                        return TryEvaluateResult.SuccessConstant;
                                }
                            }

                            // if we have either, we may be able to check undesirable cases
                            if (vocal && (haveFirst || haveSecond))
                            {
                                switch (binary.BinaryExpressionType)
                                {
                                    case BinaryExpressionType.Add:
                                    case BinaryExpressionType.Subtract:
                                        if (haveFirst && first == 0)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.Multiply:
                                        if (haveFirst && first is 0 or 1)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        if (haveSecond && second is 0 or 1)
                                        {
                                            parser.OnTrivialOperand(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                    case BinaryExpressionType.Divide:
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnDivideByZero(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        else
                                        {
                                            if (haveFirst && first == 0)
                                            {
                                                parser.OnTrivialOperand(binary.FirstExpression);
                                                hasWarnings = true;
                                            }
                                            if (haveSecond && second == 1)
                                            {
                                                parser.OnTrivialOperand(binary.SecondExpression);
                                                hasWarnings = true;
                                            }
                                        }
                                        break;
                                    case BinaryExpressionType.Modulo:
                                        if (haveSecond && second == 0)
                                        {
                                            parser.OnDivideByZero(binary.SecondExpression);
                                            hasWarnings = true;
                                        }
                                        else if (haveFirst && first == 0)
                                        {
                                            parser.OnTrivialOperand(binary.FirstExpression);
                                            hasWarnings = true;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch { } // overflow etc
            value = default;
            return hasWarnings ? TryEvaluateResult.FailureNonConstantWithWarnings : TryEvaluateResult.FailureNonConstant;
        }

        public override void Visit(TopRowFilter node)
        {
            if (node.Expression is ScalarExpression scalar)
            {
                switch (IsInt32(scalar, out var i))
                {
                    case TryEvaluateResult.SuccessConstant:
                        if (i <= 0)
                        {
                            parser.OnNonPositiveTop(scalar);
                        }
                        break;
                    case TryEvaluateResult.FailureNonConstant:
                        if (IsDecimal(scalar, out var d) == TryEvaluateResult.SuccessConstant)
                        {
                            if (!node.Percent)
                            {
                                parser.OnNonIntegerTop(scalar);
                            }
                            if (d <= 0)
                            {   // don't expect to see this; parser rejects them
                                parser.OnNonPositiveTop(scalar);
                            }
                        }
                        break;
                }
                base.Visit(node);
            }
        }

        bool _expressionAlreadyEvaluated;

        bool TrySimplifyExpression(ScalarExpression node)
        {
            if (_expressionAlreadyEvaluated) return true;

            if (node is UnaryExpression { UnaryExpressionType: UnaryExpressionType.Negative, Expression: IntegerLiteral or NumericLiteral })
            {
                // just "-4" or similar; don't warn the user to simplify that!
                _expressionAlreadyEvaluated = true;
                return false;
            }

            switch (IsInt32(node, out var intValue, vocal: true))
            {
                case TryEvaluateResult.SuccessConstant:
                    parser.OnSimplifyExpression(node, intValue);
                    _expressionAlreadyEvaluated = true;
                    break;
                case TryEvaluateResult.FailureNonConstantWithWarnings:
                    _expressionAlreadyEvaluated = true;
                    break;
                case TryEvaluateResult.FailureNonConstant:
                    // retry as decimal
                    switch (IsDecimal(node, out var decimalValue, vocal: true))
                    {
                        case TryEvaluateResult.SuccessConstant:
                            parser.OnSimplifyExpression(node, decimalValue);
                            _expressionAlreadyEvaluated = true;
                            break;
                        case TryEvaluateResult.FailureNonConstantWithWarnings:
                            _expressionAlreadyEvaluated = true;
                            break;
                    }
                    break;
            }

            return false;
        }
        public override void ExplicitVisit(UnaryExpression node)
        {
            bool restore = TrySimplifyExpression(node);
            base.ExplicitVisit(node);
            _expressionAlreadyEvaluated = restore;
        }

        public override void ExplicitVisit(BinaryExpression node)
        {
            bool restore = TrySimplifyExpression(node);
            base.ExplicitVisit(node);
            _expressionAlreadyEvaluated = restore;
        }

        public override void Visit(OutputClause node)
        {
            // note that this doesn't handle OUTPUT INTO,
            // which is via OutputIntoClause

            AddQuery(); // works like a query
            base.Visit(node);
        }

        private bool IsUnfiltered(FromClause from, WhereClause where)
        {
            if (where is not null) return false;
            return !IsMultiTable(from); // treat multi-table as filtered
        }
        private bool IsMultiTable(FromClause? from)
        {
            var tables = from?.TableReferences;
            // treat multiple tables as filtered (let's not discuss outer joins etc)
            if (tables is null || tables.Count == 0) return false;
            return tables.Count > 1 || tables[0] is JoinTableReference;
        }

        public override void ExplicitVisit(UpdateSpecification node)
        {
            Debug.Assert(!_demandAliases.Active);
            var oldDemandAlias = _demandAliases;
            try
            {
                // set ambient state so we can complain more as we walk the nodes
                _demandAliases = IsMultiTable(node.FromClause) ? new(true, node.Target) : default;
                base.ExplicitVisit(node);
                if (_demandAliases.Active && !_demandAliases.AmnestyNodeIsAlias)
                    parser.OnFromMultiTableMissingAlias(node.Target);
            }
            finally
            {
                _demandAliases = oldDemandAlias;
            }
        }
        public override void ExplicitVisit(DeleteSpecification node)
        {
            Debug.Assert(!_demandAliases.Active);
            var oldDemandAlias = _demandAliases;
            try
            {
                // set ambient state so we can complain more as we walk the nodes
                _demandAliases = IsMultiTable(node.FromClause) ? new(true, node.Target) : default;
                base.ExplicitVisit(node);
                if (_demandAliases.Active && !_demandAliases.AmnestyNodeIsAlias)
                    parser.OnFromMultiTableMissingAlias(node.Target);
            }
            finally
            {
                _demandAliases = oldDemandAlias;
            }
        }

        private struct DemandAliasesState
        {
            public DemandAliasesState(bool active, TableReference? amnesty)
            {
                Active = active;
                Amnesty = amnesty;
                AmnestyNodeIsAlias = false;
                HuntingAlias = null;
                if (amnesty is NamedTableReference { Alias: null } named && named.SchemaObject is { Count: 1 } schema)
                {
                    HuntingAlias = schema[0].Value;
                }
            }
            public readonly string? HuntingAlias;
            public readonly bool Active;
            public readonly TableReference? Amnesty; // we can't validate the target until too late
            public bool AmnestyNodeIsAlias;

            public ScalarExpression? IgnoreNode { get; set; }
        }

        private DemandAliasesState _demandAliases;

        public override void Visit(TableReferenceWithAlias node)
        {
            if (_demandAliases.Active)
            {
                if (ReferenceEquals(node, _demandAliases.Amnesty))
                {
                    // ignore for now
                }
                else if (string.IsNullOrWhiteSpace(node.Alias?.Value))
                {
                    parser.OnFromMultiTableMissingAlias(node);
                }
                else if (node.Alias!.Value == _demandAliases.HuntingAlias)
                {
                    // we've resolved the Target node as an alias
                    _demandAliases.AmnestyNodeIsAlias = true;
                }
            }
            base.Visit(node);
        }
        public override void Visit(ColumnReferenceExpression node)
        {
            if (_demandAliases.Active && node.MultiPartIdentifier.Count == 1
                && !ReferenceEquals(node, _demandAliases.IgnoreNode))
            {
                parser.OnFromMultiTableUnqualifiedColumn(node, node.MultiPartIdentifier[0].Value);
            }
            base.Visit(node);
        }

        public override void Visit(DeleteSpecification node)
        {
            if (IsUnfiltered(node.FromClause, node.WhereClause))
            {
                parser.OnDeleteWithoutWhere(node);
            }
            base.Visit(node);
        }

        public override void Visit(UpdateSpecification node)
        {
            if (IsUnfiltered(node.FromClause, node.WhereClause))
            {
                parser.OnUpdateWithoutWhere(node);
            }
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
                if (node.ExecutableEntity is ExecutableStringList list)
                {
                    if (list.Strings.Count == 0)
                    { } // ??
                    else if (list.Strings.Count == 1 && list.Strings[0] is StringLiteral)
                    { } // we'll let them off
                    else
                    {
                        parser.OnExecComposedSql(node);
                    }
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
                    if (p.IsOutput && p.ParameterValue is VariableReference variableRef)
                    {
                        var variable = MarkAssigned(variableRef, false);
                        if (variable.IsTable)
                        {
                            parser.OnTableVariableOutputParameter(variable);
                        }
                    }
                }
            }
        }

        public override void ExplicitVisit(ExecuteParameter node)
        {
            Visit(node);
            // node.Variable?.Accept(this); // this isn't our variable; don't demand it exists
            if (node.IsOutput && node.ParameterValue is VariableReference variable)
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
                parser.OnGlobalIdentity(node);
            }
            base.Visit(node);
        }

        public override void Visit(InsertSpecification node)
        {
            var knownInsertedCount = TryCount(node.InsertSource, out var count, out bool unbalanced);
            if (unbalanced)
            {
                parser.OnInsertColumnsUnbalanced(node.InsertSource);
            }
            if (node.Columns.Count == 0)
            {
                parser.OnInsertColumnsNotSpecified(node);
            }
            else if (knownInsertedCount && count != node.Columns.Count)
            {
                parser.OnInsertColumnMismatch(node.InsertSource);
            }

            base.Visit(node);

            static bool TryCount(InsertSource insertSource, out int count, out bool unbalanced)
            {
                unbalanced = false;
                switch (insertSource)
                {
                    case ValuesInsertSource values when values.RowValues.Count > 0:
                        count = values.RowValues[0].ColumnValues.Count;
                        // check they're all the same width!
                        foreach (var row in values.RowValues)
                        {
                            if (row.ColumnValues.Count != count)
                            {
                                unbalanced = true;
                                return false;
                            }
                        }
                        return true;
                    case SelectInsertSource select when select.Select is QuerySpecification expr:
                        count = expr.SelectElements.Count;
                        return true;
                }
                count = 0;
                return false;
            }
        }

        private Variable MarkAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, true);
        private Variable EnsureAssigned(VariableReference node, bool isTable)
            => EnsureOrMarkAssigned(node, isTable, false);
        private Variable EnsureOrMarkAssigned(VariableReference node, bool isTable, bool markAssigned)
        {
            if (TryGetVariable(node.Name, out var existing))
            {
                if (existing.IsUnusedParameter)
                {
                    existing = existing.WithUsed(); // we found it!
                }

                var blame = existing.WithLocation(node);
                if (markAssigned)
                {
                    if (existing.IsUnconsumed && !existing.IsTable)
                    {
                        if (AssignmentTracking) parser.OnVariableValueNotConsumed(blame);
                    }
                    // mark as has value + unconsumed
                    existing = existing.WithUnconsumedValue().WithLocation(node);
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
                        existing = existing.WithConsumed();
                    }
                }
                return SetVariable(existing);
            }
            else
            {
                // assume this is a local we don't know about; we'll figure out if it is a parameter afterwards
                var flags = isTable ? (VariableFlags.Table | VariableFlags.UsedInQuery) : VariableFlags.UsedInQuery;
                if (markAssigned && !isTable) flags |= VariableFlags.Unconsumed;

                return SetVariable(new(node, flags));
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
}

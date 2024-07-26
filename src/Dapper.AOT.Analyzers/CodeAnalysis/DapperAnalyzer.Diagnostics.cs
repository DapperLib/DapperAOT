using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class DapperAnalyzer
{
    internal sealed class Diagnostics : DiagnosticsBase
    {
        public static readonly DiagnosticDescriptor
        // general usage
        // 000: InterceptorsGenerated
        UnsupportedMethod = LibraryInfo("DAP001", "Unsupported method", "The Dapper method '{0}' is not currently supported by Dapper.AOT"),
        // unused: DAP002
        // 003: InterceptorsNotEnabled
        // 004: LanguageVersionTooLow
        DapperAotNotEnabled = LibraryWarning("DAP005", "Dapper.AOT not enabled", "{0} candidate Dapper methods detected, but none have Dapper.AOT enabled"),
        DapperLegacyTupleParameter = LibraryWarning("DAP006", "Dapper tuple-type parameter", "Dapper (original) does not work well with tuple-type parameters as name information is inaccessible"),
        UnexpectedCommandType = LibraryInfo("DAP007", "Unexpected command type", "The command type specified is not understood"),
        // unused: DAP008
        UnexpectedArgument = LibraryInfo("DAP009", "Unexpected parameter", "The parameter '{0}' is not understood"),
        // unused: DAP010
        DapperLegacyBindNameTupleResults = LibraryWarning("DAP011", "Named-tuple results", "Dapper (original) does not support tuple results with bind-by-name semantics"),
        DapperAotAddBindTupleByName = LibraryWarning("DAP012", "Add BindTupleByName", "Because of differences in how Dapper and Dapper.AOT can process tuple-types, please add '[BindTupleByName({true|false})]' to clarify your intent"),
        DapperAotTupleResults = LibraryInfo("DAP013", "Tuple-type results", "Tuple-type results are not currently supported"),
        DapperAotTupleParameter = LibraryInfo("DAP014", "Tuple-type parameter", "Tuple-type parameters are not currently supported"),
        UntypedParameter = LibraryInfo("DAP015", "Untyped parameter", "The parameter type could not be resolved"),
        GenericTypeParameter = LibraryInfo("DAP016", "Generic type parameter", "Generic type parameters ({0}) are not currently supported"),
        NonPublicType = LibraryInfo("DAP017", "Non-accessible type", "Type '{0}' is not accessible; {1} types are not currently supported"),
        SqlParametersNotDetected = SqlWarning("DAP018", "SQL parameters not detected", "Parameters are being supplied, but no parameters were detected in the command"),
        // unused: DAP019
        // unused: DAP020
        DuplicateParameter = LibraryWarning("DAP021", "Duplicate parameter", "Members '{0}' and '{1}' both have the database name '{2}'; '{1}' will be ignored"),
        DuplicateReturn = LibraryWarning("DAP022", "Duplicate return parameter", "Members '{0}' and '{1}' are both designated as return values; '{1}' will be ignored"),
        DuplicateRowCount = LibraryWarning("DAP023", "Duplicate row-count member", "Members '{0}' and '{1}' are both marked [RowCount]"),
        RowCountDbValue = LibraryWarning("DAP024", "Member is both row-count and mapped value", "Member '{0}' is marked both [RowCount] and [DbValue]; [DbValue] will be ignored"),
        ExecuteCommandWithQuery = SqlWarning("DAP025", "Execute command with query", "The command has a query that will be ignored"),
        QueryCommandMissingQuery = SqlError("DAP026", "Query/scalar command lacks query", "The command lacks a query"),
        UseSingleRowQuery = PerformanceWarning("DAP027", "Use single-row query", "Use {0}() instead of Query(...).{1}()"),
        UseQueryAsList = PerformanceWarning("DAP028", "Use AsList instead of ToList", "Use Query(...).AsList() instead of Query(...).ToList()"),
        RowCountHintRedundant = LibraryInfo("DAP029", "Row-count hint redundant", "The method-level [RowCountHint] will be ignored due to parameter member '{0}'"),
        RowCountHintInvalidValue = LibraryError("DAP030", "Row-count hint invalid value", "The [RowCountHint] parameters are invalid; a positive integer must be supplied"),
        RowCountHintShouldNotSpecifyValue = LibraryError("DAP031", "Row-count hint should not specify value", "The [RowCountHint] parameters are invalid; no parameter should be supplied"),
        RowCountHintDuplicated = LibraryError("DAP032", "Row-count hint duplicated", "Only a single member or parameter should be marked [RowCountHint]; '{0} will be ignored"),
        // 033: CommandPropertyNotFound
        // 034: CommandPropertyReserved
        ConstructorMultipleExplicit = LibraryError("DAP035", "Multiple explicit constructors", "Only one constructor should be marked [ExplicitConstructor] for type '{0}'"),
        ConstructorAmbiguous = LibraryError("DAP036", "Ambiguous constructors", "Type '{0}' has more than 1 constructor; mark one constructor with [ExplicitConstructor] or reduce constructors"),
        UserTypeNoSettableMembersFound = LibraryError("DAP037", "No settable members exist for user type", "Type '{0}' has no settable fields or properties"),
        ValueTypeSingleFirstOrDefaultUsage = LibraryWarning("DAP038", "Value-type single row 'OrDefault' usage", "Type '{0}' is a value-type; it will not be trivial to identify missing rows from {1}"),
        FactoryMethodMultipleExplicit = LibraryError("DAP039", "Multiple explicit factory methods", "Only one factory method should be marked [ExplicitConstructor] for type '{0}'"),
        ConstructorOverridesFactoryMethod = LibraryWarning("DAP041", "Constructor overrides factory method", "Type '{0}' has both constructor and factory method; Constructor will be used instead of a factory method"),
        ParameterNameOverrideConflict = LibraryWarning("DAP042", "Parameter name override conflict", "A column name is specified via both [DbValue] and [Column]; '{0}' will be used"),
        UseColumnAttributeNotSpecified = LibraryWarning("DAP043", "[Column] has no effect", "Attach the [UseColumnAttribute] attribute to make Dapper consider [Column]"),
        CancellationNotSupported = LibraryWarning("DAP044", "Cancellation not supported", "Vanilla Dapper does not support cancellation parameters in this scenario"),
        CancellationDuplicated = LibraryWarning("DAP045", "Duplicate cancellation", "Multiple parameter values cannot define cancellation"),
        AmbiguousProperties = LibraryWarning("DAP046", "Ambiguous properties", "Properties have same name '{0}' after normalization and can be conflated"),
        AmbiguousFields = LibraryWarning("DAP047", "Ambiguous fields", "Fields have same name '{0}' after normalization and can be conflated"),
        MoveFromDbString = LibraryWarning("DAP048", "Move from DbString to DbValue", "DbString achieves the same as [DbValue] does. Use it instead."),

        // SQL parse specific
        GeneralSqlError = SqlWarning("DAP200", "SQL error", "SQL error: {0}"),
        MultipleBatches = SqlError("DAP201", "Multiple batches", "Multiple batches are not permitted"),
        DuplicateVariableDeclaration = SqlError("DAP202", "Duplicate variable declaration", "The variable {0} is declared multiple times"),
        GlobalIdentity = SqlError("DAP203", "Do not use @@identity", "@@identity should not be used; prefer SCOPE_IDENTITY() or OUTPUT INSERTED.yourid"),
        SelectScopeIdentity = SqlInfo("DAP204", "Prefer OUTPUT over SELECT", "Consider using OUTPUT INSERTED.yourid in the INSERT instead of SELECT SCOPE_IDENTITY()"),
        NullLiteralComparison = SqlWarning("DAP205", "Null comparison", "Literal null used in comparison; 'is null' or 'is not null' should be preferred"),
        ParseError = SqlError("DAP206", "SQL parse error", "SQL parse error {0}: '{1}'"),
        ScalarVariableUsedAsTable = SqlError("DAP207", "Scalar used like table", "Scalar variable {0} is used like a table"),
        TableVariableUsedAsScalar = SqlError("DAP208", "Table used like scalar", "Table-variable {0} is used like a scalar"),
        TableVariableAccessedBeforePopulate = SqlError("DAP209", "Table used before populate", "Table-variable {0} is accessed before it populated"),
        VariableAccessedBeforeAssignment = SqlError("DAP210", "Variable used before assigned", "Variable {0} is accessed before it is assigned a value"),
        VariableAccessedBeforeDeclaration = SqlError("DAP211", "Variable used before declared", "Variable {0} is accessed before it is declared"),
        ExecComposedSql = SqlWarning("DAP212", "EXEC with composed SQL", "EXEC with composed SQL may be susceptible to SQL injection; consider EXEC sp_executesql, taking care to fully parameterize the composed query"),
        VariableValueNotConsumed = SqlWarning("DAP213", "Variable value not consumed", "Variable {0} has a value that is not consumed"),
        VariableNotDeclared = SqlError("DAP214", "Variable not declared", "Variable {0} is not declared and no corresponding parameter exists"),
        TableVariableOutputParameter = SqlWarning("DAP215", "Table variable used as output parameter", "Table variable {0} cannot be used as an output parameter"),
        InsertColumnsNotSpecified = SqlWarning("DAP216", "INSERT without target columns", "INSERT should explicitly specify target columns"),
        InsertColumnsMismatch = SqlError("DAP217", "INSERT with mismatched columns", "The INSERT values do not match the target columns"),
        InsertColumnsUnbalanced = SqlError("DAP218", "INSERT with unbalanced rows", "The INSERT rows have different widths"),
        SelectStar = SqlWarning("DAP219", "SELECT with wildcard columns", "SELECT columns should be specified explicitly"),
        SelectEmptyColumnName = SqlWarning("DAP220", "SELECT with missing column name", "SELECT column name is missing: {0}"),
        SelectDuplicateColumnName = SqlWarning("DAP221", "SELECT with duplicate column name", "SELECT column name is duplicated: '{0}'"),
        SelectAssignAndRead = SqlWarning("DAP222", "SELECT with assignment and reads", "SELECT statement assigns variable and performs reads"),
        DeleteWithoutWhere = SqlWarning("DAP223", "DELETE without WHERE", "DELETE statement lacks WHERE clause"),
        UpdateWithoutWhere = SqlWarning("DAP224", "UPDATE without WHERE", "UPDATE statement lacks WHERE clause"),
        FromMultiTableMissingAlias = SqlWarning("DAP225", "Multi-element FROM missing alias", "FROM expressions with multiple elements should use aliases"),
        FromMultiTableUnqualifiedColumn = SqlWarning("DAP226", "Multi-element FROM with unqualified column", "FROM expressions with multiple elements should qualify all columns; it is unclear where '{0}' is located"),
        NonIntegerTop = SqlError("DAP227", "Non-integer TOP", "TOP literals should be integers"),
        NonPositiveTop = SqlError("DAP228", "Non-positive TOP", "TOP literals should be positive"),
        SelectFirstTopError = SqlWarning("DAP229", "SELECT for First* with invalid TOP", "SELECT for First* should use TOP 1"),
        SelectSingleTopError = SqlWarning("DAP230", "SELECT for Single* with invalid TOP", "SELECT for Single* should use TOP 2; if you do not need to test over-read, use First*"),
        SelectSingleRowWithoutWhere = SqlWarning("DAP231", "SELECT for single row without WHERE", "SELECT for single row without WHERE or (TOP and ORDER BY)"),
        NonPositiveFetch = SqlError("DAP232", "Non-positive FETCH", "FETCH literals should be positive"),
        NegativeOffset = SqlError("DAP233", "Negative OFFSET", "OFFSET literals should be non-negative"),
        SimplifyExpression = SqlInfo("DAP234", "Expression can be simplified", "Expression evaluates to a constant and can be replaced with '{0}'"),
        TopWithOffset = SqlError("DAP235", "TOP with OFFSET clause", "TOP cannot be used in a query with OFFSET; use FETCH instead"),
        UnusedParameter = SqlWarning("DAP236", "Unused parameter", "Parameter '{0}' is not used, but will be included"),
        DivideByZero = SqlWarning("DAP237", "Divide by zero", "Division by zero detected"),
        TrivialOperand = SqlWarning("DAP238", "Trivial operand", "Operand makes this calculation trivial; it can be simplified"),
        InvalidNullExpression = SqlWarning("DAP239", "Invalid null expression", "Operation requires a non-null operand"),
        VariableParameterConflict = SqlError("DAP240", "Parameter/variable conflict", "The declaration of variable '{0}' conflicts with a parameter"),
        InterpolatedStringSqlExpression = SqlWarning("DAP241", "Interpolated string usage", "Data values should not be interpolated into SQL string - use parameters instead"),
        ConcatenatedStringSqlExpression = SqlWarning("DAP242", "Concatenated string usage", "Data values should not be concatenated into SQL string - use parameters instead"),
        InvalidDatepartToken = SqlWarning("DAP243", "Valid datepart token expected", "Date functions require a recognized datepart argument"),
        SelectAggregateMismatch = SqlWarning("DAP244", "SELECT aggregate mismatch", "SELECT has mixture of aggregate and non-aggregate expressions"),
        PseudoPositionalParameter = SqlError("DAP245", "Avoid SQL pseudo-positional parameter", "It is more like Dapper will incorrectly treat this literal as a pseudo-positional parameter")
        ;
    }
}

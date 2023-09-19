using Microsoft.CodeAnalysis;

namespace Dapper.CodeAnalysis;

partial class DapperAnalyzer
{
    internal sealed class Diagnostics : DiagnosticsBase
    {
        public static readonly DiagnosticDescriptor
        // general usage
        UnsupportedMethod = LibraryInfo("DAP001", "Unsupported method", "The Dapper method '{0}' is not currently supported by Dapper.AOT", true),
        DapperAotNotEnabled = LibraryInfo("DAP005", "Dapper.AOT not enabled", "{0} candidate Dapper methods detected, but none have Dapper.AOT enabled", true),
        UseSingleRowQuery = PerformanceWarning("DAP027", "Use single-row query", "Use {0}() instead of Query(...).{1}()", true),
        UseQueryAsList = PerformanceWarning("DAP028", "Use AsList instead of ToList", "Use Query(...).AsList() instead of Query(...).ToList()", true),

        // SQL parse specific
        GeneralSqlError = SqlWarning("DAP200", "SQL error", "SQL error: {0}"),
        MultipleBatches = SqlError("DAP201", "Multiple batches", "Multiple batches are not permitted"),
        DuplicateVariableDeclaration = SqlError("DAP202", "Duplicate variable declaration", "The variable {0} is declared multiple times"),
        GlobalIdentity = SqlError("DAP203", "Do not use @@identity", "@@identity should not be used; prefer SCOPE_IDENTITY() or OUTPUT INSERTED.yourid"),
        SelectScopeIdentity = SqlInfo("DAP204", "Prefer OUTPUT over SELECT", "Consider using OUTPUT INSERTED.yourid in the INSERT instead of SELECT SCOPE_IDENTITY()"),
        NullLiteralComparison = SqlWarning("DAP205", "Null comparison", "Literal null used in comparison; 'is null' or 'is not null' should be preferred"),
        ParseError = SqlError("DAP206", "SQL parse error", "Error {0}: {1}"),
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
        SelectSingleRowWithoutWhere = SqlWarning("DAP231", "SELECT for single row without WHERE", "SELECT for single row without WHERE or (TOP and ORDER BY)");
    }
}

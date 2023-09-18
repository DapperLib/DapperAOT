namespace Dapper;

// THIS FILE MUST BE KEPT IN SYNC BETWEEN THE LIB AND ANALYZERS

/// <summary>
/// Indicates the family of SQL variant used
/// </summary>
public enum SqlSyntax
{
    // RESERVED: 0

    /// <summary>
    /// General purpose SQL; handles most common variants, but with low fidelity; multiple parameter
    /// conventions may work, including <c>@value</c> and <c>:value</c>
    /// </summary>
    General = 1,

    /// <summary>
    /// General purpose SQL, using the <c>@value</c> parameter convention
    /// </summary>
    GeneralWithAtParameters = 2,

    /// <summary>
    /// General purpose SQL, using the <c>:value</c> parameter convention
    /// </summary>
    GeneralWithColonParameters = 3,

    /// <summary>
    /// SQL Server (<a href="https://learn.microsoft.com/sql/t-sql/language-reference">Transact-SQL</a>)using the <c>@value</c> parameter convention; has full syntax processing support
    /// </summary>
    SqlServer = 100,

    /// <summary>
    /// PostgreSQL, using the <c>:value</c> parameter convention
    /// </summary>
    PostgreSql = 200,

    /// <summary>
    /// MySQL and MariaDB, using the <c>@value</c> parameter convention
    /// </summary>
    MySql = 300,

    /// <summary>
    /// Oracle, using the <c>:value</c> parameter convention
    /// </summary>
    Oracle = 400,

    /// <summary>
    /// SQLite, using the <c>@value</c> parameter convention
    /// </summary>
    SQLite = 500,
}
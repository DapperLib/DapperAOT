using Dapper.Internal;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dapper;

/// <summary>
/// Provides extension methods to help perform ADO.NET operations
/// </summary>
public static class DapperAotExtensions
{
    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, commandType, timeout, handler);

    /// <summary>
    /// Coerce a pending sequence of values to <see cref="IEnumerable{T}"/>
    /// </summary>
    public static async Task<IEnumerable<TValue>> AsEnumerableAsync<TValue>(Task<List<TValue>> values) => await values;

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(DbConnection connection, DbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(IDbConnection connection, IDbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, (DbTransaction?)transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this IDbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, null, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this IDbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, (DbTransaction)transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Suggest an appropriate command-type for the provided SQL
    /// </summary>
    public static CommandType GetCommandType(string sql)
        => CompiledRegex.WhitespaceOrReserved.IsMatch(sql) ? CommandType.Text : CommandType.StoredProcedure;
}


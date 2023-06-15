using System.Collections;
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
    public static Command<T> Command<T>(this DbConnection connection, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Coerce a pending sequence of values to <see cref="IEnumerable{T}"/>
    /// </summary>
    public static async Task<IEnumerable<TValue>> AsEnumerableAsync<TValue>(Task<List<TValue>> values) => await values;

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbTransaction transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, transaction, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(DbConnection connection, DbTransaction? transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, transaction, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(IDbConnection connection, IDbTransaction? transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, (DbTransaction?)transaction, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this IDbConnection connection, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, null, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this IDbTransaction transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, (DbTransaction)transaction, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(this DbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(DbConnection connection, DbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(IDbConnection connection, IDbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, (DbTransaction?)transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(this IDbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new((DbConnection)connection, null, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a batch that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Batch<T> Batch<T>(this IDbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(null, (DbTransaction)transaction, sql, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(connection, null, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this DbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(null, transaction, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this DbConnection connection, DbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(connection, transaction, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this IDbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new((DbConnection)connection, null, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this IDbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(null, (DbTransaction)transaction, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this IDbConnection connection, IDbTransaction? transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new((DbConnection)connection, (DbTransaction?)transaction, sql, null!, commandType, timeout, handler);
}


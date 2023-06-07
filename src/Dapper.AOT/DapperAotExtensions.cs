using Dapper.Internal;
using System.Data;
using System.Data.Common;

namespace Dapper;

/// <summary>
/// Provides extension methods to help perform ADO.NET operations
/// </summary>
public static class DapperAotExtensions
{
    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbTransaction transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(CommandUtils.TypeCheck(transaction.Connection), transaction, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbConnection connection, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, args, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this DbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(CommandUtils.TypeCheck(transaction.Connection), transaction, sql, null!, commandType, timeout, handler);

    /// <summary>
    /// Create a command that does not take parameters
    /// </summary>
    public static Command<object> Command(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(connection, null, sql, null!, commandType, timeout, handler);
}


using Dapper.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using static Dapper.Command;

namespace Dapper;

/// <summary>
/// Provides extension methods to help perform ADO.NET operations
/// </summary>
public static partial class DapperAotExtensions
{
    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbConnection connection, string sql, params scoped ReadOnlySpan<Parameter> parameters)
        => new(connection, null, sql, 0, 0, parameters);

    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbTransaction transaction, string sql, params scoped ReadOnlySpan<Parameter> parameters)
        => new(null, transaction, sql, 0, 0, parameters);

    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbConnection connection, string sql, scoped ReadOnlySpan<Parameter> parameters = default, CommandType commandType = 0, int timeout = 0)
        => new(connection, null, sql, commandType, timeout, parameters);

    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbTransaction transaction, string sql, scoped ReadOnlySpan<Parameter> parameters = default, CommandType commandType = 0, int timeout = 0)
        => new(null, transaction, sql, commandType, timeout, parameters);

    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbConnection connection, string sql, Parameter[]? parameters, CommandType commandType = 0, int timeout = 0)
        => new(connection, null, sql, commandType, timeout, parameters);

    /// <summary>
    /// Create a command that takes explicit parameters (or no parameters).
    /// </summary>
    public static Command Command(this DbTransaction transaction, string sql, Parameter[]? parameters, CommandType commandType = 0, int timeout = 0)
        => new(null, transaction, sql, commandType, timeout, parameters);

    /// <summary>
    /// Create a command that takes parameters from <typeparamref name="T"/>
    /// </summary>
    public static Command<T> Command<T>(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, commandType, timeout, handler);

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
    /// Coerce a pending sequence of values to <see cref="IEnumerable{T}"/>
    /// </summary>
    public static async Task<IEnumerable<TValue>> AsEnumerableAsync<TValue>(Task<List<TValue>> values) => await values;

    /// <summary>
    /// Suggest an appropriate command-type for the provided SQL
    /// </summary>
    public static CommandType GetCommandType(string sql)
        => CompiledRegex.WhitespaceOrReserved.IsMatch(sql) ? CommandType.Text : CommandType.StoredProcedure;
}


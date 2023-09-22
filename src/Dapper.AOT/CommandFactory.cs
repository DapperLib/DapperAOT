﻿using Dapper.Internal;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dapper;

/// <summary>
/// Allows an implementation to control how an ADO.NET command is constructed
/// </summary>
public abstract class CommandFactory
{
    /// <summary>
    /// Provides a basic determination of whether a parameter is used in a query and should be included
    /// </summary>
    protected static bool Include(string sql, CommandType commandType, string name)
    {
        Debug.Assert(sql is not null);
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }
        if (commandType == CommandType.Text)
        {
            int startIndex = 0;
            while (true)
            {
                var found = sql!.IndexOf(name, startIndex, StringComparison.OrdinalIgnoreCase);
                if (found < 0) return false; // not found

                if (found != 0 && sql[found - 1] is '@' or '$' or ':' or '?')
                {
                    // so definitely have @foo (or similar) - exclude over-hits
                    if (sql.Length == found + name.Length)
                    {
                        return true;
                    }
                    // test the *next* char to see if OK
                    char c = sql[found + name.Length];
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        // part of another variable; keep looking
                    }
                    else
                    {
                        return true;
                    }
                }
                startIndex = found + 1;
            }
        }
        else
        {
            return true;
        }
    }

    private static readonly object[] s_BoxedInt32 = new object[] { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    private static readonly object s_BoxedTrue = true, s_BoxedFalse = false;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(int value)
        => value >= -1 && value <= 10 ? s_BoxedInt32[value + 1] : value;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(int? value)
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(bool value)
        => value ? s_BoxedTrue : s_BoxedFalse;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(bool? value)
        => value.HasValue ? (value.GetValueOrDefault() ? s_BoxedTrue : s_BoxedFalse) : DBNull.Value;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue<T>(T? value) where T : struct
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(object? value)
        => value ?? DBNull.Value;


    /// <summary>
    /// Wrap a value for use in an ADO.NET parameter
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object AsGenericValue<T>(T value)
    {
        // note JIT elide here
        if (typeof(T) == typeof(bool)) return AsValue(Unsafe.As<T, bool>(ref value));
        if (typeof(T) == typeof(bool?)) return AsValue(Unsafe.As<T, bool?>(ref value));
        if (typeof(T) == typeof(int)) return AsValue(Unsafe.As<T, int>(ref value));
        if (typeof(T) == typeof(int?)) return AsValue(Unsafe.As<T, int?>(ref value));
        return AsValue((object?)value);
    }

    /// <summary>
    /// Flexibly parse an <see cref="object"/> as a value of type <typeparamref name="T"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Parse<T>(object? value) => CommandUtils.As<T>(value);

    /// <summary>
    /// Allows the caller to cast values by providing a template shape - in particular this allows anonymous types to be accessed in type-safe code
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for type inference")]
    protected static T Cast<T>(object? value, Func<T> shape) => (T)value!;

    internal abstract void PostProcessObject(DbCommand command, object? args, int rowCount);

    /// <summary>
    /// Gets a shared command-factory with minimal command processing
    /// </summary>
    public static CommandFactory<object?> Simple  => CommandFactory<object?>.Default;

    /// <summary>
    /// Indicates whether this command is suitable for <see cref="DbCommand.Prepare"/> usage
    /// </summary>
    public virtual bool CanPrepare => false;

    /// <summary>
    /// Provides an opportunity to recycle and reuse command instances
    /// </summary>
    public virtual bool TryRecycle(DbCommand command) => false;

    /// <summary>
    /// Provides an opportunity to recycle and reuse command instances
    /// </summary>
    protected static bool TryRecycle(ref DbCommand? storage, DbCommand command)
    {
        // detach and recycle
        command.Connection = null;
        command.Transaction = null;
        return Interlocked.CompareExchange(ref storage, command, null) is null;
    }
}

/// <summary>
/// Allows an implementation to control how an ADO.NET command is constructed
/// </summary>
public class CommandFactory<T> : CommandFactory
{
    internal static readonly CommandFactory<T> Default = new();

    /// <summary>
    /// Create a new instance
    /// </summary>
    protected CommandFactory() { }

    /// <summary>
    /// Provide and configure an ADO.NET command that can perform the requested operation
    /// </summary>
#pragma warning disable IDE0079 // unnecessary suppression; it is necessary on some TFMs
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2249:Consider using 'string.Contains' instead of 'string.IndexOf'", Justification = "Not available in all target frameworks; readability is fine")]
#pragma warning restore IDE0079
    public virtual DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, T args)
    {
        // default behavior assumes no args, no special logic
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = commandType != 0 ? commandType : sql.IndexOf(' ') >= 0 ? CommandType.Text : CommandType.StoredProcedure; // assume text if at least one space
        AddParameters(cmd, args);
        return cmd;
    }

    internal override sealed void PostProcessObject(DbCommand command, object? args, int rowCount) => PostProcess(command, (T)args!, rowCount);

    /// <summary>
    /// Allows an implementation to process output parameters etc after an operation has completed
    /// </summary>
    public virtual void PostProcess(DbCommand command, T args, int rowCount) { }

    /// <summary>
    /// Add parameters with values
    /// </summary>
    public virtual void AddParameters(DbCommand command, T args)
    {
    }

    /// <summary>
    /// Update parameter values
    /// </summary>
    public virtual void UpdateParameters(DbCommand command, T args)
    {
        if (command.Parameters.Count != 0) // try to avoid rogue "dirty" checks
        {
            command.Parameters.Clear();
        }
        AddParameters(command, args);
    }

    /// <summary>
    /// Provides an opportunity to recycle and reuse command instances
    /// </summary>
    protected DbCommand? TryReuse(ref DbCommand? storage, string sql, CommandType commandType, T args)
    {
        var cmd = Interlocked.Exchange(ref storage, null);
        if (cmd is not null)
        {
            // try to avoid any dirty detection in the setters
            if (cmd.CommandText != sql) cmd.CommandText = sql;
            if (cmd.CommandType != commandType) cmd.CommandType = commandType;
            UpdateParameters(cmd, args);
        }
        return cmd;
    }
}
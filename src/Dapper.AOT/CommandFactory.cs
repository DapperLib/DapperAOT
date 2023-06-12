using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    /// Allows the caller to cast values by providing a template shape - in particular this allows anonymous types to be accessed in type-safe code
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Used for type inference")]
    protected static T Cast<T>(object? value, Func<T> shape) => (T)value!;

    /// <summary>
    /// Gets a shared command-factory with minimal command processing
    /// </summary>
    public static CommandFactory<object?> Simple  => CommandFactory<object?>.Default;
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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2249:Consider using 'string.Contains' instead of 'string.IndexOf'", Justification = "Not available in all target frameworks; readability is fine")]
    public virtual DbCommand GetCommand(DbConnection connection, string sql, CommandType commandType, T args)
    {
        // default behavior assumes no args, no special logic
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = commandType != 0 ? commandType : sql.IndexOf(' ') >= 0 ? CommandType.Text : CommandType.StoredProcedure; // assume text if at least one space
        AddParameters(cmd, args);
        return cmd;
    }
    /// <summary>
    /// Allows an implementation to process output parameters etc after an operation has completed
    /// </summary>
    public virtual void PostProcess(DbCommand command, T args) { }

    /// <summary>
    /// Allows an implementation to process output parameters etc after an operation has completed
    /// </summary>
    public virtual void PostProcess(DbCommand command, T args, int rowCount) => PostProcess(command, args);

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
        command.Parameters.Clear();
        AddParameters(command, args);
    }


    /// <summary>
    /// Provides an opportunity to recycle and reuse command instances
    /// </summary>
    public virtual bool TryRecycle(DbCommand command) => false;
}
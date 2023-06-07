using Dapper.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

public readonly struct Command<TArgs>
{
    private readonly CommandType commandType;
    private readonly int timeout;
    private readonly string sql;
    private readonly DbConnection connection;
    private readonly DbTransaction? transaction;
    private readonly CommandFactory<TArgs> commandFactory;
    private readonly TArgs args;

    public Command(IDbConnection connection, IDbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
        : this(Utils.TypeCheck(connection), Utils.TypeCheck(transaction), sql, args, commandType, timeout, commandFactory) {}

    public Command(DbConnection connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, [DapperAot] CommandFactory<TArgs>? commandFactory = null)
    {
        this.commandFactory = commandFactory ?? CommandFactory<TArgs>.Default;
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(connection);
#else
        if (connection is null) throw new ArgumentNullException(nameof(connection));
#endif
        this.connection = connection;
        this.transaction = transaction;
        this.sql = sql;
        this.args = args;
        this.commandType = commandType;
        this.timeout = timeout;
    }

    private void PostProcessCommand(ref DbCommand? command)
    {
        Debug.Assert(command is not null);
        if (commandFactory.PostProcess(command!, args))
        {
            command = null;
        }
    }

    private DbCommand GetCommand()
    {
        var cmd = commandFactory.Prepare(connection!, sql, commandType, args);
        cmd.Connection = connection;
        cmd.Transaction = transaction;
        if (timeout >= 0)
        {
            cmd.CommandTimeout = timeout;
        }
        return cmd;
    }

    private bool OpenIfNeeded()
    {
        if (connection.State == ConnectionState.Open)
        {
            return false;
        }
        else
        {
            connection.Open();
            return true;
        }
    }

    private ValueTask<bool> OpenIfNeededAsync(CancellationToken cancellationToken)
    {
        if (connection.State == ConnectionState.Open)
        {
            return default;
        }
        else
        {
            var pending = connection.OpenAsync(cancellationToken);
            return pending.IsCompletedSuccessfully()
                ? new(true) : Awaited(pending);

            static async ValueTask<bool> Awaited(Task pending)
            {
                await pending;
                return true;
            }
        }
    }

    public TRow QuerySingle<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory);

    public TRow QueryFirst<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfNone, rowFactory);

    public TRow? QuerySingleOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.ThrowIfMultiple, rowFactory);

    public TRow? QueryFirstOrDefault<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
        => QueryOneRow(OneRowFlags.None, rowFactory);

    public Task<TRow> QuerySingleAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfNone | OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken);

    public Task<TRow> QueryFirstAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfNone, rowFactory, cancellationToken);

    public Task<TRow?> QuerySingleOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.ThrowIfMultiple, rowFactory, cancellationToken);

    public Task<TRow?> QueryFirstOrDefaultAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
        => QueryOneRowAsync(OneRowFlags.None, rowFactory, cancellationToken);

    public int Execute()
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = OpenIfNeeded();
            cmd = GetCommand();
            var result = cmd.ExecuteNonQuery();
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            Utils.Cleanup(null, cmd, connection, closeConn);
        }
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = await OpenIfNeededAsync(cancellationToken);
            cmd = GetCommand();
            var result = await cmd.ExecuteNonQueryAsync(cancellationToken);
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            await Utils.CleanupAsync(null, cmd, connection, closeConn);
        }
    }

    public T ExecuteScalar<T>()
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = OpenIfNeeded();
            cmd = GetCommand();
            var result = cmd.ExecuteScalar();
            PostProcessCommand(ref cmd);
            return Utils.As<T>(result);
        }
        finally
        {
            Utils.Cleanup(null, cmd, connection, closeConn);
        }
    }

    public async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        try
        {
            closeConn = await OpenIfNeededAsync(cancellationToken);
            cmd = GetCommand();
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            PostProcessCommand(ref cmd);
            return Utils.As<T>(result);
        }
        finally
        {
            await Utils.CleanupAsync(null, cmd, connection, closeConn);
        }
    }

    public IEnumerable<TRow> Query<TRow>(bool buffered, [DapperAot] RowFactory<TRow>? rowFactory = null)
        => buffered ? QueryBuffered(rowFactory) : QueryUnbuffered(rowFactory);

    public List<TRow> QueryBuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            var results = new List<TRow>();
            if (reader.Read())
            {
                int[]? leased = null;
                var fieldCount = reader.FieldCount;
                var readWriteTokens
                    = fieldCount == 0 ? default
                    : fieldCount <= MAX_STACK_TOKENS ? Utils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], fieldCount)
                    : Utils.UnsafeRent(out leased, fieldCount);

                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, readWriteTokens, 0);
                ReadOnlySpan<int> readOnlyTokens = readWriteTokens; // avoid multiple conversions
                do
                {
                    results.Add(rowFactory.Read(reader, readOnlyTokens, 0));
                }
                while (reader.Read());
                Utils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
            return results;
        }
        finally
        {
            Utils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    public async Task<List<TRow>> QueryBufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            var results = new List<TRow>();
            if (await reader.ReadAsync(cancellationToken))
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, Utils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    results.Add(rowFactory.Read(reader, Utils.UnsafeReadOnlySpan(leased, fieldCount), 0));
                }
                while (await reader.ReadAsync(cancellationToken));
                Utils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
            return results;
        }
        finally
        {
            await Utils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }

    public async IAsyncEnumerable<TRow> QueryUnbufferedAsync<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null, CancellationToken cancellationToken = default)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            if (await reader.ReadAsync(cancellationToken))
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, Utils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    yield return rowFactory.Read(reader, Utils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                }
                while (await reader.ReadAsync(cancellationToken));
                Utils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
        }
        finally
        {
            await Utils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }

    public IEnumerable<TRow> QueryUnbuffered<TRow>([DapperAot] RowFactory<TRow>? rowFactory = null)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;
        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            if (reader.Read())
            {
                var fieldCount = reader.FieldCount;
                (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, Utils.UnsafeRent(out var leased, fieldCount), 0);
                do
                {
                    yield return rowFactory.Read(reader, Utils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                }
                while (reader.Read());
                Utils.Return(leased);
            }
            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
        }
        finally
        {
            Utils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    const int MAX_STACK_TOKENS = 64;

    private TRow QueryOneRow<TRow>(
        OneRowFlags oneRowFlags,
        RowFactory<TRow>? rowFactory)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;

        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) == 0)
            {   // if we don't care if there's two rows, we can restrict to read one only
                behavior |= CommandBehavior.SingleRow;
            }
            if (closeConn = OpenIfNeeded())
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = cmd.ExecuteReader(behavior);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TRow result = default!;
            if (reader.Read())
            {
                {   // extra scope level so the compiler can ensure we aren't using the lease beyond the expected lifetime
                    int[]? leased = null;
                    var fieldCount = reader.FieldCount;
                    var readWriteTokens
                        = fieldCount == 0 ? default
                        : fieldCount <= MAX_STACK_TOKENS ? Utils.UnsafeSlice(stackalloc int[MAX_STACK_TOKENS], fieldCount)
                        : Utils.UnsafeRent(out leased, fieldCount);

                    (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, readWriteTokens, 0);
                    result = rowFactory.Read(reader, readWriteTokens, 0);
                    Utils.Return(leased);
                }

                if (reader.Read())
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        Utils.ThrowMultiple();
                    }
                    while (reader.Read()) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                Utils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (reader.NextResult()) { }
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            Utils.Cleanup(reader, cmd, connection, closeConn);
        }
    }

    private async Task<TRow> QueryOneRowAsync<TRow>(
        OneRowFlags oneRowFlags,
        RowFactory<TRow>? rowFactory,
        CancellationToken cancellationToken)
    {
        bool closeConn = false;
        DbCommand? cmd = null;
        DbDataReader? reader = null;

        try
        {
            var behavior = CommandBehavior.SingleResult | CommandBehavior.SequentialAccess;
            if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) == 0)
            {   // if we don't care if there's two rows, we can restrict to read one only
                behavior |= CommandBehavior.SingleRow;
            }
            if (closeConn = await OpenIfNeededAsync(cancellationToken))
            {
                behavior |= CommandBehavior.CloseConnection;
            }
            cmd = GetCommand();
            reader = await cmd.ExecuteReaderAsync(behavior, cancellationToken);
            closeConn = false; // handled by CommandBehavior.CloseConnection

            TRow result = default!;
            if (await reader.ReadAsync(cancellationToken))
            {
                {   // extra scope level so the compiler can ensure we aren't using the lease beyond the expected lifetime
                    var fieldCount = reader.FieldCount;
                    (rowFactory ??= RowFactory<TRow>.Default).Tokenize(reader, Utils.UnsafeRent(out var leased, fieldCount), 0);

                    result = rowFactory.Read(reader, Utils.UnsafeReadOnlySpan(leased, fieldCount), 0);
                    Utils.Return(leased);
                }

                if (await reader.ReadAsync(cancellationToken))
                {
                    if ((oneRowFlags & OneRowFlags.ThrowIfMultiple) != 0)
                    {
                        Utils.ThrowMultiple();
                    }
                    while (await reader.ReadAsync(cancellationToken)) { }
                }
            }
            else if ((oneRowFlags & OneRowFlags.ThrowIfNone) != 0)
            {
                Utils.ThrowNone();
            }

            // consume entire results (avoid unobserved TDS error messages)
            while (await reader.NextResultAsync(cancellationToken)) { }
            PostProcessCommand(ref cmd);
            return result;
        }
        finally
        {
            await Utils.CleanupAsync(reader, cmd, connection, closeConn);
        }
    }
}

public static class DapperAotExtensions
{
    public static Command<T> Command<T>(this DbTransaction transaction, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(transaction.Connection, transaction, sql, args, commandType, timeout, handler);

    public static Command<T> Command<T>(this DbConnection connection, string sql, T args, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<T>? handler = null)
        => new(connection, null, sql, args, commandType, timeout, handler);

    public static Command<object> Command(this DbTransaction transaction, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(transaction.Connection, transaction, sql, null!, commandType, timeout, handler);
    public static Command<object> Command(this DbConnection connection, string sql, CommandType commandType = 0, int timeout = 0, [DapperAot] CommandFactory<object>? handler = null)
        => new(connection, null, sql, null!, commandType, timeout, handler);
}

public abstract class CommandFactory
{
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
                var found = sql.IndexOf(name, startIndex, StringComparison.OrdinalIgnoreCase);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(int value)
        => value >= -1 && value <= 10 ? s_BoxedInt32[value + 1] : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(int? value)
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(bool value)
        => value ? s_BoxedTrue : s_BoxedFalse;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(bool? value)
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue<T>(T? value) where T : struct
        => value.HasValue ? AsValue(value.GetValueOrDefault()) : DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static object AsValue(object? value)
        => value ?? DBNull.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T Cast<T>(object? value, Func<T> shape) => (T)value!;
}
public class CommandFactory<T> : CommandFactory
{
    internal static readonly CommandFactory<T> Default = new();
    protected CommandFactory() { }

    public virtual DbCommand Prepare(DbConnection connection, string sql, CommandType commandType, T args)
    {
        // default behavior assumes no args, no special logic
        var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = commandType != 0 ? commandType : sql.IndexOf(' ') >= 0 ? CommandType.Text : CommandType.StoredProcedure; // assume text if at least one space
        return cmd;
    }

    // process out etc parameters; return "true" if the command should not be disposed
    public virtual bool PostProcess(DbCommand command, T args) => false; // nothing to do
}


public abstract class RowFactory
{
    protected static T GetValue<T>(DbDataReader reader, int fieldOffset)
        => Utils.As<T>(reader.GetValue(fieldOffset));

    protected static uint NormalizedHash(string? value) => StringHashing.NormalizedHash(value);

    protected static bool NormalizedEquals(string? value, string? normalized) => StringHashing.NormalizedEquals(value, normalized);
}

public class RowFactory<T> : RowFactory
{
    internal static readonly RowFactory<T> Default = new();
    protected RowFactory() { }
    public virtual void Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset) { }
    public virtual T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset)
        => reader.GetFieldValue<T>(columnOffset);
}

internal static class Utils
{
#if NETCOREAPP3_1_OR_GREATER
    const MethodImplOptions AggressiveOptions = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
    const MethodImplOptions AggressiveOptions = MethodImplOptions.AggressiveInlining;
#endif

    /// <summary>
    /// Asserts that the connection provided is usable
    /// </summary>
    [MethodImpl(AggressiveOptions)]
    public static DbConnection TypeCheck(IDbConnection cnn)
    {
        if (cnn is not DbConnection typed)
        {
            Throw(cnn);
        }
        return typed;
        static void Throw(IDbConnection cnn)
        {
            if (cnn is null) throw new ArgumentNullException(nameof(cnn));
            throw new ArgumentException("The supplied connection must be a " + nameof(DbConnection), nameof(cnn));
        }
    }

    /// <summary>
    /// Asserts that the transaction provided is usable
    /// </summary>
    [MethodImpl(AggressiveOptions)]
    public static DbTransaction? TypeCheck(IDbTransaction? transaction)
    {
        if (transaction is null) return null;
        if (transaction is not DbTransaction typed)
        {
            Throw();
        }
        return typed;
        static void Throw() => throw new ArgumentException("The supplied transaction must be a " + nameof(DbTransaction), nameof(transaction));
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowNone() => _ = System.Linq.Enumerable.First("");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowMultiple() => System.Linq.Enumerable.Single("  ");

    [MethodImpl(AggressiveOptions)]
    internal static ReadOnlySpan<int> UnsafeReadOnlySpan(int[] value, int length)
    {
        Debug.Assert(value is not null);
        Debug.Assert(length >= 0 && length <= value.Length);
#if NET8_0_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(value), length);
#else
        return new ReadOnlySpan<int>(value, 0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static Span<int> UnsafeSlice(Span<int> value, int length)
    {
        Debug.Assert(length >= 0 && length <= value.Length);
#if NETCOREAPP3_1_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(value), length);
#else
        return value.Slice(0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static Span<int> UnsafeRent(out int[] leased, int length)
    {
        Debug.Assert(length >= 0);
        leased = ArrayPool<int>.Shared.Rent(length);
#if NET8_0_OR_GREATER && !DEBUG
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(leased), length);
#else
        return new Span<int>(leased, 0, length);
#endif
    }

    [MethodImpl(AggressiveOptions)]
    internal static void Return(int[]? leased)
    {
        if (leased is not null)
        {
            ArrayPool<int>.Shared.Return(leased);
        }
    }

    [MethodImpl(AggressiveOptions)]
    internal static void Cleanup(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        reader?.Dispose();
        command?.Dispose();
        if (closeConnection)
        {
            connection.Close();
        }
    }

#if NETCOREAPP3_1_OR_GREATER
    internal static async ValueTask CleanupAsync(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        if (reader is not null)
        {
            await reader.DisposeAsync();
        }
        if (command is not null)
        {
            await command.DisposeAsync();
        }
        if (closeConnection)
        {
            await connection.CloseAsync();
        }
    }
#else
    [MethodImpl(AggressiveOptions)]
    internal static ValueTask CleanupAsync(DbDataReader? reader, DbCommand? command, DbConnection connection, bool closeConnection)
    {
        Cleanup(reader, command, connection, closeConnection);
        return default;
    }
#endif

    [MethodImpl(AggressiveOptions)]
    internal static bool IsCompletedSuccessfully(this Task task)
    {
#if NETCOREAPP3_1_OR_GREATER
        return task.IsCompletedSuccessfully;
#else
        return task.Status == TaskStatus.RanToCompletion;
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowNull() => throw new ArgumentNullException("value");

    [MethodImpl(AggressiveOptions)]
    internal static T As<T>(object? value)
    {
        if (value is null or DBNull)
        {
            // if value-typed and *not* Nullable<T>, then: that's an error
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null)
            {
                ThrowNull();
            }
            return default!;
        }
        else
        {
            if (value is T typed)
            {
                return typed;
            }

            // note we're using value-type T JIT dead-code removal to elide most of these checks
            if (typeof(T) == typeof(int))
            {
                int t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<int, T>(ref t);
            }
            if (typeof(T) == typeof(int?))
            {
                int? t = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<int?, T>(ref t);
            }
            else if (typeof(T) == typeof(bool))
            {
                bool t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return Unsafe.As<bool, T>(ref t);
            }
            else if (typeof(T) == typeof(bool?))
            {
                bool? t = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                return Unsafe.As<bool?, T>(ref t);
            }
            else if (typeof(T) == typeof(float))
            {
                float t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return Unsafe.As<float, T>(ref t);
            }
            else if (typeof(T) == typeof(float?))
            {
                float? t = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                return Unsafe.As<float?, T>(ref t);
            }
            else if (typeof(T) == typeof(double))
            {
                double t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Unsafe.As<double, T>(ref t);
            }
            else if (typeof(T) == typeof(double?))
            {
                double? t = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return Unsafe.As<double?, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal))
            {
                decimal t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return Unsafe.As<decimal, T>(ref t);
            }
            else if (typeof(T) == typeof(decimal?))
            {
                decimal? t = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return Unsafe.As<decimal?, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime))
            {
                DateTime t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                return Unsafe.As<DateTime, T>(ref t);
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                DateTime? t = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                return Unsafe.As<DateTime?, T>(ref t);
            }
            else if (typeof(T) == typeof(long))
            {
                long t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<long, T>(ref t);
            }
            else if (typeof(T) == typeof(long?))
            {
                long? t = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<long?, T>(ref t);
            }
            else if (typeof(T) == typeof(short))
            {
                short t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<short, T>(ref t);
            }
            else if (typeof(T) == typeof(short?))
            {
                short? t = Convert.ToInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<short?, T>(ref t);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                sbyte t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<sbyte, T>(ref t);
            }
            else if (typeof(T) == typeof(sbyte?))
            {
                sbyte? t = Convert.ToSByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<sbyte?, T>(ref t);
            }
            else if (typeof(T) == typeof(ulong))
            {
                ulong t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ulong, T>(ref t);
            }
            else if (typeof(T) == typeof(ulong?))
            {
                ulong? t = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ulong?, T>(ref t);
            }
            else if (typeof(T) == typeof(uint))
            {
                uint t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<uint, T>(ref t);
            }
            else if (typeof(T) == typeof(uint?))
            {
                uint? t = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                return Unsafe.As<uint?, T>(ref t);
            }
            else if (typeof(T) == typeof(ushort))
            {
                ushort t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ushort, T>(ref t);
            }
            else if (typeof(T) == typeof(ushort?))
            {
                ushort? t = Convert.ToUInt16(value, CultureInfo.InvariantCulture);
                return Unsafe.As<ushort?, T>(ref t);
            }
            else if (typeof(T) == typeof(byte))
            {
                byte t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<byte, T>(ref t);
            }
            else if (typeof(T) == typeof(byte?))
            {
                byte? t = Convert.ToByte(value, CultureInfo.InvariantCulture);
                return Unsafe.As<byte?, T>(ref t);
            }
            else if (typeof(T) == typeof(char))
            {
                char t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                return Unsafe.As<char, T>(ref t);
            }
            else if (typeof(T) == typeof(char?))
            {
                char? t = Convert.ToChar(value, CultureInfo.InvariantCulture);
                return Unsafe.As<char?, T>(ref t);
            }
            // this won't elide, but: we'll live with it
            else if (typeof(T) == typeof(string))
            {
                var t = Convert.ToString(value, CultureInfo.InvariantCulture)!;
                return Unsafe.As<string, T>(ref t);
            }
            else
            {
                return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
            }
        }
    }
}

[Flags]
internal enum OneRowFlags
{
    None = 0,
    ThrowIfNone = 1 << 0,
    ThrowIfMultiple = 1 << 1,
}

#pragma warning restore CS1591
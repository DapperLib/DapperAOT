using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

#pragma warning disable IDE0079 // following will look unnecessary on up-level
#pragma warning disable CS1574 // DbBatchCommand will not resolve on down-level TFMs
/// <summary>
/// Represents the state associated with a <see cref="System.Data.Common.DbCommand"/> or <see cref="System.Data.Common.DbBatchCommand"/> (where supported).
/// </summary>
/// <remarks>Only the current command is available to the caller.</remarks>
#pragma warning disable CS1574
#pragma warning restore IDE0079

public readonly struct UnifiedCommand

{
    // could be:
    // a) a DbCommand instance for a single operation
    // b) a DbBatch instance for a multi-command operation using the DbBatch API
    // c) a List<DbCommand> instance for a multi-command operation using the legacy API
    private readonly object _source;

    private readonly CommandFactory _commandFactory;

    internal CommandFactory CommandFactory => _commandFactory;

    // 0 for "a"; for "b" and "c", this is the index of the current operation
    private readonly int _index;

#if NET6_0_OR_GREATER
    // may be used by the multi-command API to create new parameters
    private readonly DbCommand? _spareCommandForParameters;
#endif

    /*
    private object? Source => _source switch
    {
        DbCommand cmd => cmd,
        List<DbCommand> list => list[_index],
#if NET6_0_OR_GREATER
        DbBatch batch => batch.BatchCommands[_index],
#endif
        _ => null,
    };
    */

    internal int CommandCount => _source switch
    {
        DbCommand cmd => 1,
        List<DbCommand> list => list.Count,
#if NET6_0_OR_GREATER
        DbBatch batch => batch.BatchCommands.Count,
#endif
        _ => 0,
    };

    /// <inheritdoc/>
    public override string ToString() => CommandText;

    /// <inheritdoc cref="DbCommand.Connection"/>
    internal DbConnection? Connection => _source switch
    {
        DbCommand cmd => cmd.Connection,
        List<DbCommand> list => list[_index].Connection,
#if NET6_0_OR_GREATER
        DbBatch batch => batch.Connection,
#endif
        _ => null,
    };

    /// <inheritdoc cref="DbCommand.Transaction"/>
    internal DbTransaction? Transaction => _source switch
    {
        DbCommand cmd => cmd.Transaction,
        List<DbCommand> list => list[_index].Transaction,
#if NET6_0_OR_GREATER
        DbBatch batch => batch.Transaction,
#endif
        _ => null,
    };

    /// <summary>
    /// The <see cref="System.Data.Common.DbCommand"/> associated with the current operation, if appropriate.
    /// </summary>
    public DbCommand? Command => _source switch
    {
        DbCommand cmd => cmd,
        List<DbCommand> list => list[_index],
        _ => null,
    };

#if NET6_0_OR_GREATER
    /// <summary>
    /// The <see cref="System.Data.Common.DbBatchCommand"/> associated with the current operation, if appropriate.
    /// </summary>
    public DbBatchCommand? BatchCommand => _source switch
    {
        DbBatch batch => batch.BatchCommands[_index],
        _ => null,
    };
    /// <summary>
    /// The <see cref="System.Data.Common.DbBatch"/> associated with the current operation, if appropriate.
    /// </summary>
    internal DbBatch? Batch => _source as DbBatch;



#endif

    /// <inheritdoc cref="DbCommand.CommandText"/>
    public string CommandText
    {
        get => _source switch
        {
            DbCommand cmd => cmd.CommandText,
            List<DbCommand> list => list[_index].CommandText,
#if NET6_0_OR_GREATER
            DbBatch batch => batch.BatchCommands[_index].CommandText,
#endif
            _ => "",
        };
        [Obsolete("When possible, " + nameof(SetCommand) + " should be preferred", false)]
        set
        {
            switch (_source)
            {
                case DbCommand cmd:
                    cmd.CommandText = value;
                    break;
                case List<DbCommand> list:
                    list[_index].CommandText = value;
                    break;
#if NET6_0_OR_GREATER
                case DbBatch batch:
                    batch.BatchCommands[_index].CommandText = value;
                    break;
#endif
            }
        }
    }

    internal bool IsDefault => _source is null;

    /// <inheritdoc cref="DbCommand.Parameters"/>
    public DbParameterCollection Parameters
    {
        get {
            return _source switch
            {
                DbCommand cmd => cmd.Parameters,
                List<DbCommand> list => list[_index].Parameters,
#if NET6_0_OR_GREATER
                DbBatch batch => batch.BatchCommands[_index].Parameters,
#endif
                _ => Throw(),
            };
            static DbParameterCollection Throw() => throw new InvalidOperationException("The command is not initialized");
        }
    }

    /// <inheritdoc cref="DbCommand.CommandType"/>
    public CommandType CommandType
    {
        get => _source switch
        {
            DbCommand cmd => cmd.CommandType,
            List<DbCommand> list => list[_index].CommandType,
#if NET6_0_OR_GREATER
            DbBatch batch => batch.BatchCommands[_index].CommandType,
#endif
            _ => CommandType.Text,
        };
        [Obsolete("When possible, " + nameof(SetCommand) + " should be preferred", false)]
        set
        {
            switch (_source)
            {
                case DbCommand cmd:
                    cmd.CommandType = value;
                    break;
                case List<DbCommand> list:
                    list[_index].CommandType = value;
                    break;
#if NET6_0_OR_GREATER
                case DbBatch batch:
                    batch.BatchCommands[_index].CommandType = value;
                    break;
#endif
            }
        }
    }

    /// <inheritdoc cref="DbCommand.CommandTimeout"/>
    public int TimeoutSeconds
    {
        get => _source switch
        {
            DbCommand cmd => cmd.CommandTimeout,
            List<DbCommand> list => list[_index].CommandTimeout,
#if NET6_0_OR_GREATER
            DbBatch batch => batch.Timeout,
#endif
            _ => 0,
        };
        set
        {
            switch (_source)
            {
                case DbCommand cmd:
                    cmd.CommandTimeout = value;
                    break;
                case List<DbCommand> list:
                    list[_index].CommandTimeout = value;
                    break;
#if NET6_0_OR_GREATER
                case DbBatch batch:
                    batch.Timeout = value;
                    break;
#endif
            }
        }
    }

    /// <summary>
    /// Create a parameter and add it to the parameter collection
    /// </summary>
    public DbParameter AddParameter()
    {
        var p = _commandFactory.CreateNewParameter(in this);
        Parameters.Add(p);
        return p;
    }

    /// <inheritdoc cref="DbCommand.CreateParameter"/>
#if DEBUG
    [Obsolete("Prefer " + nameof(AddParameter))]
#endif
    public DbParameter CreateParameter() => _commandFactory.CreateNewParameter(in this);

    internal DbParameter DefaultCreateParameter()
    {
        switch (_source)
        {
            case DbCommand cmd:
                return cmd.CreateParameter();
            case List<DbCommand> list:
                var activeCmd = list[_index];
                return activeCmd.CreateParameter();
#if NET6_0_OR_GREATER
            case DbBatch batch:
                var bc = batch.BatchCommands[_index];
#if NET8_0_OR_GREATER // https://github.com/dotnet/runtime/issues/82326
                if (bc.CanCreateParameter) return bc.CreateParameter();
#endif // NET8
                return (_spareCommandForParameters ?? UnsafeBatchWithCommandForParameters()).CreateParameter();
#endif // NET6
            default:
                return Throw();

        }
        static DbParameter Throw() => throw new InvalidOperationException("It was not possible to create a parameter for this command");
    }

    internal UnifiedCommand(CommandFactory commandFactory, DbCommand command)
    {
        _source = command;
        _index = 0;
#if NET6_0_OR_GREATER
        _spareCommandForParameters = null;
#endif
        _commandFactory = commandFactory;
    }

#if NET6_0_OR_GREATER

    internal UnifiedCommand(CommandFactory commandFactory, DbBatch batch)
    {
        _source = batch;
        _spareCommandForParameters = null;

        var bc = batch.BatchCommands;
        if (bc.Count == 0)
        {
            // initialize the first command
            bc.Add(commandFactory.CreateNewCommand(batch));
        }
        _index = 0;
        _commandFactory = commandFactory;
    }

    private DbCommand UnsafeBatchWithCommandForParameters()
    {
        return _spareCommandForParameters
            ?? (Connection is { } conn ? (Unsafe.AsRef(in _spareCommandForParameters) = _commandFactory.CreateNewCommand(conn)) : null)
            ?? Throw();
        static DbCommand Throw() => throw new InvalidOperationException("It was not possible to create command parameters for this batch; the connection may be null");
    }
#endif

    internal int RecordsAffected
    {
        get
        {
#if NET6_0_OR_GREATER
            if (_source is DbBatch batch)
            {
                return batch.BatchCommands[_index].RecordsAffected;
            }
#endif
            return -1;
        }
    }

    /// <summary>
    /// Creates a new command and moves into that context
    /// </summary>
    internal DbParameterCollection AddCommand(string? commandText, CommandType commandType)
    {
        DbParameterCollection? result = null;
        switch (_source)
        {
            case DbCommand cmd when cmd.Connection is not null:
                if (Index != 0) ThrowNotLast();
                // swap for a list, then!
                var newCmd = _commandFactory.CreateNewCommand(cmd.Connection);
                newCmd.Connection = cmd.Connection;
                newCmd.Transaction = cmd.Transaction;
                if (commandText is not null)
                {
                    newCmd.CommandText = commandText;
                    newCmd.CommandType = commandType;
                }
                Unsafe.AsRef(in _source) = new List<DbCommand> { cmd, newCmd };
                result = newCmd.Parameters;
                break;
            case List<DbCommand> list:
                if (Index != list.Count - 1) ThrowNotLast();
                foreach (var item in list)
                {
                    if (item.Connection is not null)
                    {
                        newCmd = _commandFactory.CreateNewCommand(item.Connection);
                        newCmd.Connection = item.Connection;
                        newCmd.Transaction = item.Transaction;
                        if (commandText is not null)
                        {
                            newCmd.CommandText = commandText;
                            newCmd.CommandType = commandType;
                        }
                        list.Add(newCmd);
                        result = newCmd.Parameters;
                        break;
                    }
                }
                break;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                if (Index != batch.BatchCommands.Count - 1) ThrowNotLast();
                var bc = _commandFactory.CreateNewCommand(batch);
                if (commandText is not null)
                {
                    bc.CommandText = commandText;
                    bc.CommandType = commandType;
                }
                batch.BatchCommands.Add(bc);
                result = bc.Parameters;
                break;
#endif
        }
        if (result is null) Throw();
        Unsafe.AsRef(in _index)++;
        return result!;

        static void Throw() => throw new NotSupportedException("It was not possible to create a new command in this batch; the connection may be invalid");
        static void ThrowNotLast() => throw new InvalidOperationException(nameof(AddCommand) + " would overwrite existing commands; command may be incorrectly positioned");
    }

    internal int Index => _index;

    internal void UnsafeMoveToFinal() => Unsafe.AsRef(in _index) = CommandCount - 1;
    internal void UnsafeMoveTo(int index) => Unsafe.AsRef(in _index) = index;
    internal void UnsafeAdvance() => Unsafe.AsRef(in _index)++;

    internal DbParameterCollection this[int index]
    {
        get
        {
            return _source switch
            {
                DbCommand cmd when index == 0 => cmd.Parameters,
                List<DbCommand> list => list[index].Parameters,
#if NET6_0_OR_GREATER
                DbBatch batch => batch.BatchCommands[index].Parameters,
#endif
                _ => Throw()
            };
            static DbParameterCollection Throw() => throw new IndexOutOfRangeException();
        }
    }
    internal void Cleanup()
    {
#if NET6_0_OR_GREATER
        var spare = _spareCommandForParameters;
#endif
        var source = _source;
        Unsafe.AsRef(in this) = default; // best efforts to prevent double-stomp

#if NET6_0_OR_GREATER
        spare?.Dispose();
#endif

        switch (source)
        {
            case DbCommand cmd:
                cmd.Dispose();
                break;
            case List<DbCommand> list:
                foreach (var cmd in list)
                {
                    cmd.Dispose();
                }
                break;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                batch.Dispose();
                break;
#endif
        }
    }

    internal void Trim() // removes all *excess* commands
    {
        switch (_source)
        {
            case List<DbCommand> list:
                int remove = list.Count - (_index + 1);
                if (remove > 0)
                {
                    list.RemoveRange(_index + 1, remove);
                }
                break;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                var bc = batch.BatchCommands;
                for (int i = bc.Count - 1; i > _index; i--)
                {
                    bc.RemoveAt(i);
                }
                break;
#endif
        }
    }

    internal void Prepare()
    {
        switch (_source)
        {
            case DbCommand cmd:
                cmd.Prepare();
                break;
            case List<DbCommand> list:
                foreach (var cmd in list)
                {
                    cmd.Prepare();
                }
                break;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                batch.Prepare();
                break;
#endif
        }
    }

    internal Task PrepareAsync(CancellationToken cancellationToken)
    {
        return _source switch
        {
#if NETCOREAPP3_0_OR_GREATER
            DbCommand cmd => cmd.PrepareAsync(cancellationToken),
#else
            DbCommand cmd => PrepareSingleAsync(cmd),
#endif
            List<DbCommand> list => PrepareAsync(list, cancellationToken),
#if NET6_0_OR_GREATER
            DbBatch batch => batch.PrepareAsync(cancellationToken),
#endif
            _ => Task.CompletedTask,
        };

#if NETCOREAPP3_0_OR_GREATER

        static async Task PrepareAsync(List<DbCommand> list, CancellationToken cancellationToken)
        {
            foreach (var cmd in list)
                await cmd.PrepareAsync(cancellationToken);
        }
#else
// best we can do without the missing API
        static Task PrepareSingleAsync(DbCommand cmd)
        {
            cmd.Prepare();
            return Task.CompletedTask;
        }
        static Task PrepareAsync(List<DbCommand> list, CancellationToken _)
        {
            foreach (var cmd in list)
                cmd.Prepare();
            return Task.CompletedTask;
        }
#endif


    }

    void AssertFinal([CallerMemberName]string caller = "")
    {
        if (Index != CommandCount - 1) Throw(caller);
        static void Throw(string caller) => throw new InvalidOperationException(caller + " can only be invoked when in the last command position");
    }

    internal int ExecuteNonQuery()
    {
        AssertFinal();
        switch (_source)
        {
            case DbCommand cmd:
                return cmd.ExecuteNonQuery();
            case List<DbCommand> list:
                int sum = 0;
                foreach (var cmd in list)
                {
                    sum += cmd.ExecuteNonQuery();
                }
                return sum;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                return batch.ExecuteNonQuery();
#endif
            default:
                return 0;
        }
    }

    internal DbDataReader ExecuteReader(CommandBehavior flags)
    {
        AssertFinal();
        return _source switch
        {
            DbCommand cmd => cmd.ExecuteReader(flags),
#if NET6_0_OR_GREATER
            DbBatch batch => batch.ExecuteReader(flags),
#endif
            null => throw new InvalidOperationException(),
            _ => throw new NotImplementedException($"ExecuteReader for {_source.GetType().Name} is not yet implemented; poke Marc"),
        };
    }

    internal Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        AssertFinal();
        return _source switch
        {
            DbCommand cmd => cmd.ExecuteNonQueryAsync(cancellationToken),
            List<DbCommand> list => ExecuteListAsync(list, cancellationToken),
#if NET6_0_OR_GREATER
            DbBatch batch => batch.ExecuteNonQueryAsync(cancellationToken),
#endif
            _ => TaskZero,
        };

        static async Task<int> ExecuteListAsync(List<DbCommand> list, CancellationToken cancellationToken)
        {
            int sum = 0;
            foreach (var cmd in list)
            {
                sum += await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            return sum;
        }
    }

    /// <summary>
    /// Initialize the <see cref="DbCommand.CommandText"/> and <see cref="DbCommand.CommandType"/>
    /// </summary>
    public void SetCommand(string commandText, CommandType commandType = CommandType.Text)
    {
        switch (_source)
        {
            // note we're trying to avoid triggering any unnecessary side-effects and
            // cache-invalidations that could be triggered from setters
            case DbCommand cmd:
                if (cmd.CommandText != commandText) cmd.CommandText = commandText;
                if (cmd.CommandType != commandType) cmd.CommandType = commandType;
                break;
            case List<DbCommand> list:
                var activeCmd = list[_index];
                if (activeCmd.CommandText != commandText) activeCmd.CommandText = commandText;
                if (activeCmd.CommandType != commandType) activeCmd.CommandType = commandType;
                break;
#if NET6_0_OR_GREATER
            case DbBatch batch:
                var bc = batch.BatchCommands[_index];
                if (bc.CommandText != commandText) bc.CommandText = commandText;
                if (bc.CommandType != commandType) bc.CommandType = commandType;
                break;
#endif
        }
    }

    internal void TryRecycle()
    {
        if (_source switch
        {
            // note we're trying to avoid triggering any unnecessary side-effects and
            // cache-invalidations that could be triggered from setters
            DbCommand cmd => _commandFactory.TryRecycle(cmd),
            // note we don't expect to recycle list usage in this way; we're only expecting
            // single-arg scenarios
#if NET6_0_OR_GREATER
            DbBatch batch => _commandFactory.TryRecycle(batch),
#endif
            _ => false,
        })
        {
            // wipe the source - someone else can see it
            Unsafe.AsRef(in _source) = null!;
        }
    }

    private static readonly Task<int> TaskZero = Task.FromResult<int>(0);
}
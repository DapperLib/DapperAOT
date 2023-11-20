using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    /// The <see cref="System.Data.Common.DbCommand"/> associated with the current operation; this may be <c>null</c> for batch commands.
    /// </summary>
    public DbCommand? Command => _source switch
    {
        DbCommand cmd => cmd,
        List<DbCommand> list => list[_index],
        _ => null,
    };

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
    public DbParameterCollection Parameters => _source switch
    {
        DbCommand cmd => cmd.Parameters,
        List<DbCommand> list => list[_index].Parameters,
#if NET6_0_OR_GREATER
        DbBatch batch => batch.BatchCommands[_index].Parameters,
#endif
        _ => null!,
    };

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
        // TODO: optimize to avoid double tests
        var p = CreateParameter();
        Parameters.Add(p);
        return p;
    }

    /// <inheritdoc cref="DbCommand.CreateParameter"/>
    public DbParameter CreateParameter()
    {
        switch (_source)
        {
            case DbCommand cmd:
                return cmd.CreateParameter();
            case List<DbCommand> list:
                return list[_index].CreateParameter();
#if NET6_0_OR_GREATER
            case DbBatch batch:
#if NET8_0_OR_GREATER // https://github.com/dotnet/runtime/issues/82326
                var bc = batch.BatchCommands[_index];
                if (bc.CanCreateParameter) return bc.CreateParameter();
#endif // NET8
                return (_spareCommandForParameters ?? UnsafeBatchWithCommandForParameters()).CreateParameter();
#endif // NET6
            default:
                return Throw();

        }
        static DbParameter Throw() => throw new InvalidOperationException("It was not possible to create a parameter for this command");
    }

    internal UnifiedCommand(DbCommand command)
    {
        _source = command;
        _index = 0;
#if NET6_0_OR_GREATER
        _spareCommandForParameters = null;
#endif
    }

#if NET6_0_OR_GREATER

    internal UnifiedCommand(DbBatch batch)
    {
        // withCommand is typically true for a ready-to-go command; it is false if, for example, we're
        // doing a multi-row exec and want to start completely empty
        _source = batch;
        _spareCommandForParameters = null;
        
        batch.BatchCommands.Add(batch.CreateBatchCommand());
        _index = 0;
    }

    private DbCommand UnsafeBatchWithCommandForParameters()
    {
        return _spareCommandForParameters
            ?? (Unsafe.AsRef(in _spareCommandForParameters) = Connection?.CreateCommand())
            ?? Throw();
        static DbCommand Throw() => throw new InvalidOperationException("It was not possible to create command parameters for this batch; the connection may be null");
    }
#endif

    internal void ClearSource() => Unsafe.AsRef(in _source) = null!;

    internal void PostProcess<T>(IEnumerable<T> source, CommandFactory<T> commandFactory)
    {
        var snapshot = _index;
        ref int index = ref Unsafe.AsRef(in _index);
        index = 0;

        foreach (var arg in source)
        {
            commandFactory.PostProcess(in this, arg, 0); // TODO: records affected
            index++;
        }

        index = snapshot;
    }

    internal void PostProcess<T>(ReadOnlySpan<T> source, CommandFactory<T> commandFactory)
    {
        var snapshot = _index;
        ref int index = ref Unsafe.AsRef(in _index);
        index = 0;

        foreach (var arg in source)
        {
            commandFactory.PostProcess(in this, arg, 0); // TODO: records affected
            index++;
        }

        index = snapshot;
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
                // swap for a list, then!
                var newCmd = cmd.Connection.CreateCommand();
                if (commandText is not null)
                {
                    newCmd.CommandText = commandText;
                    newCmd.CommandType = commandType;
                }
                Unsafe.AsRef(in _source) = new List<DbCommand> { cmd, newCmd };
                result = newCmd.Parameters;
                break;
            case List<DbCommand> list:
                foreach (var item in list)
                {
                    if (item.Connection is not null)
                    {
                        newCmd = item.Connection.CreateCommand();
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
                var bc = batch.CreateBatchCommand();
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
    }

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

    internal int ExecuteNonQuery()
    {
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
        switch (_source)
        {
            case DbCommand cmd:
                return cmd.ExecuteReader(flags);
#if NET6_0_OR_GREATER
            case DbBatch batch:
                return batch.ExecuteReader(flags);
#endif
            case null:
                throw new InvalidOperationException();
            default:
                throw new NotImplementedException($"ExecuteReader for {_source.GetType().Name} is not yet implemented; poke Marc");
        }
    }

    internal Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        switch (_source)
        {
            case DbCommand cmd:
                return cmd.ExecuteNonQueryAsync(cancellationToken);
            case List<DbCommand> list:
                return ExecuteListAsync(list, cancellationToken);
#if NET6_0_OR_GREATER
            case DbBatch batch:
                return batch.ExecuteNonQueryAsync(cancellationToken);
#endif
            default:
                return TaskZero;
        }

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

    private static readonly Task<int> TaskZero = Task.FromResult<int>(0);
}
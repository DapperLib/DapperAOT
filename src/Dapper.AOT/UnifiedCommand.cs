using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

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
    /// <summary>
    /// The <see cref="System.Data.Common.DbCommand"/> associated with the current operation; this may be <c>null</c> for batch commands.
    /// </summary>
    public DbCommand? Command
    {
        get
        {
#if NET6_0_OR_GREATER
            return batchCommand is null ? dbCommand : null;
#else
            return dbCommand;
#endif
        }
    }

    /// <inheritdoc cref="DbCommand.CommandText"/>
    public string CommandText
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batchCommand is not null) return batchCommand.CommandText;
#endif
            return AssertCommand.CommandText;
        }
        set
        {
#if NET6_0_OR_GREATER
            if (batchCommand is not null)
            {
                batchCommand.CommandText = value;
                return;
            }
#endif
            AssertCommand.CommandText = value;
        }
    }

    /// <inheritdoc cref="DbCommand.Parameters"/>
    public DbParameterCollection Parameters
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batchCommand is not null) return batchCommand.Parameters;
#endif
            return AssertCommand.Parameters;
        }
    }

    /// <inheritdoc cref="DbCommand.CommandType"/>
    public CommandType CommandType
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batchCommand is not null) return batchCommand.CommandType;
#endif
            return AssertCommand.CommandType;
        }
        set
        {
#if NET6_0_OR_GREATER
            if (batchCommand is not null)
            {
                batchCommand.CommandType = value;
                return;
            }
#endif
            AssertCommand.CommandType = value;
        }
    }

    /// <inheritdoc cref="DbCommand.CommandTimeout"/>
    public int TimeoutSeconds
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batch is not null) return batch.Timeout;
#endif
            return AssertCommand.CommandTimeout;
        }
        set
        {
#if NET6_0_OR_GREATER
            if (batch is not null)
            {
                batch.Timeout = value;
                return;
            }
#endif
            AssertCommand.CommandTimeout = value;
        }
    }

    private DbCommand AssertCommand
    {
        get
        {
            return dbCommand ?? Throw();
            static DbCommand Throw() => throw new InvalidOperationException($"The {nameof(UnifiedCommand)} is not associated with a valid command");
        }
    }

    private readonly DbCommand? dbCommand;

    /// <inheritdoc cref="DbCommand.CreateParameter"/>
    public DbParameter CreateParameter()
    {
#if NET8_0_OR_GREATER // https://github.com/dotnet/runtime/issues/82326
        if (batchCommand is { CanCreateParameter: true })
        {
            return batchCommand.CreateParameter();
        }
#endif
        return (dbCommand ?? UnsafeWithCommandForParameters()).CreateParameter();
    }

    internal UnifiedCommand(DbCommand command)
    {
        dbCommand = command;
#if NET6_0_OR_GREATER
        batchCommand = null;
        batch = null;
#endif
    }

#if NET6_0_OR_GREATER
    private readonly DbBatch? batch;
    private readonly DbBatchCommand? batchCommand;

    /// <summary>
    /// The <see cref="System.Data.Common.DbBatchCommand"/> associated with the current operation - this may be <c>null</c> for non-batch operations.
    /// </summary>
    public DbBatchCommand? BatchCommand => batchCommand;

    internal UnifiedCommand(DbBatch batch)
    {
        this.batch = batch;
        batchCommand = null;
        dbCommand = null;
    }

    internal DbBatchCommandCollection AssertBatchCommands => AssertBatch.BatchCommands;
    internal DbBatch AssertBatch
    {
        get
        {
            return batch ?? Throw();
            static DbBatch Throw() => throw new InvalidOperationException($"The {nameof(UnifiedCommand)} is not associated with a valid command-batch");
        }
    }

    internal void PostProcess<T>(IEnumerable<T> source, CommandFactory<T> commandFactory)
    {
        int i = 0;
        var commands = AssertBatchCommands;
        foreach (var arg in source)
        {
            var cmd = commands[i++];
            UnsafeSetBatchCommand(cmd);
            commandFactory.PostProcess(in this, arg, cmd.RecordsAffected);
        }
        UnsafeSetBatchCommand(null);
    }

    internal void PostProcess<T>(ReadOnlySpan<T> source, CommandFactory<T> commandFactory)
    {
        int i = 0;
        var commands = AssertBatchCommands;
        foreach (var arg in source)
        {
            var cmd = commands[i++];
            UnsafeSetBatchCommand(cmd);
            commandFactory.PostProcess(in this, arg, cmd.RecordsAffected);
        }
        UnsafeSetBatchCommand(null);
    }

    internal bool HasBatch => batch is not null;

    internal DbBatchCommand UnsafeCreateNewCommand() => Unsafe.AsRef(in batchCommand) = AssertBatch.CreateBatchCommand();

    internal void UnsafeSetBatchCommand(DbBatchCommand? value) => Unsafe.AsRef(in batchCommand) = value;
#endif

    private DbCommand UnsafeWithCommandForParameters()
    {
        return dbCommand
#if NET6_0_OR_GREATER
            ?? (Unsafe.AsRef(in dbCommand) = batch?.Connection?.CreateCommand())
#endif
            ?? Throw();
        static DbCommand Throw() => throw new InvalidOperationException("It was not possible to create command parameters for this batch; the connection may be null");
    }

    internal void Cleanup()
    {
        dbCommand?.Dispose();
#if NET6_0_OR_GREATER
        batch?.Dispose();
#endif
    }
}
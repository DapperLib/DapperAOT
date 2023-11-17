using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    /// <inheritdoc/>
    public override string ToString()
    {
#if NET6_0_OR_GREATER
        if (batch is not null)
        {
            return $"Batch with {batch.BatchCommands.Count} commands";
        }
#endif
        return dbCommand?.ToString() ?? "";
    }
    /// <see cref="DbCommand.Connection"/>
    internal DbConnection? Connection
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batch is not null) return batch.Connection;
#endif
            return dbCommand?.Connection;
        }
    }

    /// <see cref="DbCommand.Transaction"/>
    internal DbTransaction? Transaction
    {
        get
        {
#if NET6_0_OR_GREATER
            if (batch is not null) return batch.Transaction;
#endif
            return dbCommand?.Transaction;
        }
    }

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

    internal DbCommand AssertCommand
    {
        get
        {
            return dbCommand ?? Throw();
            static DbCommand Throw() => throw new InvalidOperationException($"The {nameof(UnifiedCommand)} is not associated with a valid command");
        }
    }

    // this *might* be our effective command, but in the case of batches: it might also
    // be a throwaway command we created just to allow us to create parameters on down-level libs
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

    internal DbBatch? Batch => batch;
    internal DbBatchCommandCollection AssertBatchCommands => AssertBatch.BatchCommands;

    [MemberNotNullWhen(true, nameof(Batch))]
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

    [MemberNotNullWhen(true, nameof(Batch))]
    internal bool HasBatch => batch is not null;

    internal DbBatchCommand UnsafeCreateNewCommand() => Unsafe.AsRef(in batchCommand) = AssertBatch.CreateBatchCommand();

    internal void UnsafeSetBatchCommand(DbBatchCommand? value) => Unsafe.AsRef(in batchCommand) = value;
#endif

    internal void UnsafeSetCommand(DbCommand? value) => Unsafe.AsRef(in dbCommand) = value;

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
        if (dbCommand is not null)
        {
            dbCommand.Dispose();
            Unsafe.AsRef(in dbCommand) = null!;
        }
#if NET6_0_OR_GREATER
        if (batch is not null)
        {
            batch.Dispose();
            Unsafe.AsRef(in batch) = null!;
        }
#endif
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Creates a new <see cref="DbBatchCommand"/> and switch context into the <see cref="BatchCommand"/> property.
    /// </summary>
    [MemberNotNull(nameof(BatchCommand))]
    public DbParameterCollection AddBatchCommand(string commandText)
    {
        var batch = AssertBatch;
        var cmd = batch.CreateBatchCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = commandText;
        batch.BatchCommands.Add(cmd);
        UnsafeSetBatchCommand(cmd);
#pragma warning disable CS8774 // "Member must have a non-null value when exiting." - we just did that; we just can't prove it to the compiler
        return cmd.Parameters;
#pragma warning restore CS8774
    }
#endif

    internal void Prepare()
    {
#if NET6_0_OR_GREATER
        if (batch is not null)
        {
            batch.Prepare();
        }
        else
#endif
        {
            AssertCommand.Prepare();
        }
    }
}
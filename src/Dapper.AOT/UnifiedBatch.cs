using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

internal enum BatchMode
{
    None,
    SingleCommandDbCommand,
    SingleCommandDbBatch,
    MultiCommandDbCommand,
    MultiCommandDbBatchCommand,
}

#pragma warning disable IDE0079 // following will look unnecessary on up-level
#pragma warning disable CS1574 // DbBatchCommand will not resolve on down-level TFMs
/// <summary>
/// Represents the state associated with multiple <see cref="System.Data.Common.DbCommand"/> or <see cref="System.Data.Common.DbBatchCommand"/> instances (where supported).
/// </summary>
/// <remarks>Only the current command is available to the caller.</remarks>
#pragma warning disable CS1574
#pragma warning restore IDE0079
public readonly struct UnifiedBatch
{

    private readonly BatchMode mode;
    internal readonly UnifiedCommand Command; // avoid duplication by offloading a lot of details here

    private readonly int commandStart, commandCount, groupCount; // these are used to restrict the commands that are available to a single consumer
    internal int GroupCount => groupCount;
    internal BatchMode Mode => mode;

    internal UnifiedBatch(CommandFactory commandFactory, DbCommand command)
    {
        Command = new UnifiedCommand(commandFactory, command);
        commandStart = 0;
        commandCount = groupCount = 1;
        mode = BatchMode.SingleCommandDbCommand;
        Debug.Assert(Command.CommandCount == 1);
    }

#if NET6_0_OR_GREATER
    internal UnifiedBatch(CommandFactory commandFactory, DbBatch batch)
    {
        if (batch.BatchCommands.Count == 0) Throw();

        Command = new UnifiedCommand(commandFactory, batch);
        commandStart = 0;
        commandCount = batch.BatchCommands.Count;
        groupCount = 1;
        mode = BatchMode.SingleCommandDbBatch;
        static void Throw() => throw new ArgumentException(
            message: "When creating a " + nameof(UnifiedBatch) + " for an existing batch, the batch cannot be empty",
            paramName: nameof(batch));
    }
#endif

    internal UnifiedBatch(CommandFactory commandFactory, DbConnection connection, DbTransaction? transaction)
    {
#if NET6_0_OR_GREATER
        if (connection is { CanCreateBatch: true })
        {
            var batch = commandFactory.CreateNewBatch(connection);
            batch.Connection = connection;
            batch.Transaction = transaction;
            Command = new UnifiedCommand(commandFactory, batch);
            mode = BatchMode.MultiCommandDbBatchCommand;
        }
        else
#endif
        {
            var cmd = commandFactory.CreateNewCommand(connection);
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            Command = new UnifiedCommand(commandFactory, cmd);
            mode = BatchMode.MultiCommandDbCommand;
        }
        commandStart = 0;
        commandCount = groupCount = 1;
        Debug.Assert(Command.CommandCount == 1);
    }

#if NET6_0_OR_GREATER
    internal DbBatch? Batch => Command.Batch;
#endif

    private int GetCommandIndex(int localIndex)
    {
        if (localIndex < 0 || localIndex >= commandCount) Throw();
        return commandStart + localIndex;
        static void Throw() => throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// Returns the parameters of the corresponding command
    /// </summary>
    public DbParameterCollection this[int commandIndex] => Command[GetCommandIndex(commandIndex)];

    /// <inheritdoc/>
    public override string ToString() => Command.ToString();

    /// <inheritdoc cref="DbCommand.Connection"/>
    internal DbConnection? Connection => Command.Connection;

    /// <inheritdoc cref="DbCommand.Transaction"/>
    internal DbTransaction? Transaction => Command.Transaction;

    /// <inheritdoc cref="DbCommand.CommandText"/>
    public string CommandText
    {
        get => Command.CommandText;
        [Obsolete("When possible, " + nameof(SetCommand) + " should be preferred", false)]
        set => Command.CommandText = value;
    }

    /// <summary>
    /// Initialize the <see cref="DbCommand.CommandText"/> and <see cref="DbCommand.CommandType"/>
    /// </summary>
    public void SetCommand(string commandText, CommandType commandType = CommandType.Text)
        => Command.SetCommand(commandText, commandType);

    /// <inheritdoc cref="DbCommand.CommandType"/>
    public CommandType CommandType
    {
        get => Command.CommandType;
        [Obsolete("When possible, " + nameof(SetCommand) + " should be preferred", false)]
        set => Command.CommandType = value;
    }

    /// <inheritdoc cref="DbCommand.CommandTimeout"/>
    public int TimeoutSeconds
    {
        get => Command.TimeoutSeconds;
        set => Command.TimeoutSeconds = value;
    }

    /// <inheritdoc cref="DbCommand.Parameters"/>
    public DbParameterCollection Parameters => Command.Parameters;

    /// <inheritdoc cref="DbCommand.CreateParameter"/>
#if DEBUG
    [Obsolete("Prefer " + nameof(AddParameter))]
#endif
    public DbParameter CreateParameter() => Command.CreateParameter();

    /// <inheritdoc cref="UnifiedCommand.AddParameter"/>
    public DbParameter AddParameter() => Command.AddParameter();

    internal bool IsLastCommand => Command.CommandCount == Command.Index + 1;

    /// <summary>
    /// Creates and initializes new command, returning .the parameters collection.
    /// </summary>
    public DbParameterCollection AddCommand(string commandText, CommandType commandType = CommandType.Text)
    {
        Debug.Assert(mode is BatchMode.SingleCommandDbBatch);
        return Command.AddCommand(commandText, commandType);
    }

    internal void OverwriteNextBatchGroup()
    {
        Debug.Assert(mode is BatchMode.MultiCommandDbCommand or BatchMode.MultiCommandDbBatchCommand);
        Debug.Assert(!IsLastCommand);
        Command.UnsafeAdvance();
        Unsafe.AsRef(in commandStart) = Command.Index;
        Unsafe.AsRef(in groupCount)++;
    }

    internal void CreateNextBatchGroup(string commandText, CommandType commandType)
    {
        Debug.Assert(mode is BatchMode.MultiCommandDbCommand or BatchMode.MultiCommandDbBatchCommand);
        Debug.Assert(IsLastCommand);
        AddCommand(commandText, commandType);
        Unsafe.AsRef(in commandStart) = Command.Index;
        Unsafe.AsRef(in commandCount) = 1;
        Unsafe.AsRef(in groupCount)++;
    }

    internal bool IsDefault => Command.IsDefault;

    internal CommandFactory CommandFactory => Command.CommandFactory;

    internal int ExecuteNonQuery() => GroupCount == 0 ? 0 : Command.ExecuteNonQuery();

    internal Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => GroupCount == 0 ? TaskZero : Command.ExecuteNonQueryAsync(cancellationToken);

    internal void Cleanup() => Command.Cleanup();

    internal void Trim() => Command.Trim();

    internal void TryRecycle() => Command.TryRecycle();

    internal DbDataReader ExecuteReader(CommandBehavior flags)
        => Command.ExecuteReader(flags);

    internal void Prepare() => Command.Prepare();

    internal Task PrepareAsync(CancellationToken cancellationToken) => Command.PrepareAsync(cancellationToken);

    internal void UnsafeMoveBeforeFirst()
    {
        Command.UnsafeMoveTo(-1);
        Unsafe.AsRef(in commandStart) = 0;
        Unsafe.AsRef(in commandCount) = 0;
        Unsafe.AsRef(in groupCount) = 0;
    }

    static readonly Task<int> TaskZero = Task.FromResult(0);
}

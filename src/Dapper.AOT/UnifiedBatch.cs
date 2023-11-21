using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper;

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

    internal readonly UnifiedCommand Command; // avoid duplication by offloading a lot of details here

    private readonly int commandStart, commandCount; // these are used to restrict the commands that are available to a single consumer

    internal UnifiedBatch(DbCommand command)
    {
        Command = new UnifiedCommand(command);
        commandStart = 0;
        commandCount = 1;
        Debug.Assert(Command.CommandCount == 1);
    }

#if NET6_0_OR_GREATER
    internal UnifiedBatch(DbBatch batch)
    {
        Command = new UnifiedCommand(batch);
        commandStart = 0;
        commandCount = batch.BatchCommands.Count;
        Debug.Assert(Command.CommandCount > 0); // could be multiple for batch re-use scenarios
    }
#endif

    internal UnifiedBatch(DbConnection connection, DbTransaction? transaction)
    {
#if NET6_0_OR_GREATER
        if (connection is { CanCreateBatch: true })
        {
            var batch = connection.CreateBatch();
            batch.Connection = connection;
            batch.Transaction = transaction;
            Command = new UnifiedCommand(batch);
        }
        else
#endif
        {
            var cmd = connection.CreateCommand();
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            Command = new UnifiedCommand(cmd);
        }
        commandStart = 0;
        commandCount = 1;
        Debug.Assert(Command.CommandCount == 1);
    }

    internal void PrepareToAppend()
    {
        var count = Command.CommandCount;
        Command.AddCommand(default, default);
        // move into the new slice at the end
        Unsafe.AsRef(in commandStart) = count;
        Unsafe.AsRef(in commandCount) = 1;
    }

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
    public DbParameter CreateParameter() => Command.CreateParameter();

    /// <summary>
    /// Creates and initializes new command, returning .the parameters collection.
    /// </summary>
    public DbParameterCollection AddCommand(string commandText, CommandType commandType = CommandType.Text)
    {
        var result = Command.AddCommand(commandText, commandType);
        Unsafe.AsRef(in commandCount)++;
        return result;
    }

    internal bool IsDefault => Command.IsDefault;


    internal int ExecuteNonQuery() => Command.ExecuteNonQuery();

    internal Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Command.ExecuteNonQueryAsync(cancellationToken);

    internal void Cleanup() => Command.Cleanup();

    internal void PostProcess<T>(IEnumerable<T> source, CommandFactory<T> commandFactory)
        => Command.PostProcess(source, commandFactory);

    internal void PostProcess<T>(ReadOnlySpan<T> source, CommandFactory<T> commandFactory)
        => Command.PostProcess(source, commandFactory);

    internal DbDataReader ExecuteReader(CommandBehavior flags)
        => Command.ExecuteReader(flags);

    internal void Prepare() => Command.Prepare();

    /// <inheritdoc cref="UnifiedCommand.AddParameter"/>
    public DbParameter AddParameter() => Command.AddParameter();
}

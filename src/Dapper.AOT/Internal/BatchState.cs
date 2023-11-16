#if NET6_0_OR_GREATER
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Dapper.Internal;

internal struct BatchState
{
    const int
            FLAG_CLOSE_CONNECTION = 1 << 0;

    private readonly int _flags;

    public UnifiedCommand Command; // very deliberately a public field; magic happens...

    public bool NoBatch => Command.NoBatch;

    public BatchState(DbBatch batch, int flags) : this()
    {
        _flags = flags;
        Command = new(batch);
    }

    public static BatchState Create(DbConnection connection)
    {
        Debug.Assert(connection.CanCreateBatch);
        Debug.Assert(connection is not null);
        int flags = 0;
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
            flags |= FLAG_CLOSE_CONNECTION;
        }
        return new(connection.CreateBatch(), flags);
    }

    internal static async ValueTask<BatchState> CreateAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        Debug.Assert(connection.CanCreateBatch);
        Debug.Assert(connection is not null);
        int flags = 0;
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
            flags |= FLAG_CLOSE_CONNECTION;
        }
        return new(connection.CreateBatch(), flags);
    }

    public void Cleanup()
    {
        if ((_flags & FLAG_CLOSE_CONNECTION) != 0)
        {
            Command.Connection?.Close();
        }
        Command.Cleanup();
        Unsafe.AsRef(in Command) = default;
    }

    public async ValueTask CleanupAsync()
    {
        if ((_flags & FLAG_CLOSE_CONNECTION) != 0)
        {
            if (Command.Connection is { } conn)
            {
                await conn.CloseAsync();
            }
        }
        Command.Cleanup();
        Unsafe.AsRef(in Command) = default;
    }

    public int TotalRowsAffected => _totalRowsAffected;
    public int Pending => _count;

    private int _count, _totalRowsAffected;

    public void Execute(CommandFactory commandFactory)
    {
        // TODO: all post-processing
        if (_count == 0) return;

        var batch = Command.AssertBatch;
        if (_count < batch.BatchCommands.Count) TrimExcessCommands(commandFactory);

        _totalRowsAffected += batch.ExecuteNonQuery();
        _count = 0; // reset virtual batch
    }

    public async ValueTask ExecuteAsync(CommandFactory commandFactory, CancellationToken cancellationToken)
    {
        // TODO: all post-processing; probably need async state type to avoid async locals problem
        if (_count == 0) return;

        var batch = Command.AssertBatch;
        if (_count < batch.BatchCommands.Count) TrimExcessCommands(commandFactory);

        _totalRowsAffected += await batch.ExecuteNonQueryAsync(cancellationToken);
        _count = 0; // reset virtual batch
    }

    private void TrimExcessCommands(CommandFactory commandFactory)
    {
        var commands = Command.AssertBatch.BatchCommands;
        var count = _count;
        // remove right-to-left to avoid juggling the collection
        for (int i = commands.Count - 1; i >= _count; i--)
        {
            var cmd = commands[i];
            commands.RemoveAt(i);
            // note that unlike DbCommand, DbBatchCommand is not disposable
            commandFactory.TryRecycle(cmd);
        }
        Debug.Assert(_count == commands.Count, "command trim failure");
    }

    // moves to the next command; returns true if this is a new uninitialized command, else false for reusing a command
    public void AddCommand<T>(in Command<T> command, T args)
    {
        var commands = Command.AssertBatch.BatchCommands;
        DbBatchCommand cmd;
        if (_count == commands.Count)
        {
            Command.UnsafeSetBatchCommand(null); // no need to expose old value to public API
            cmd = command.GetBatchCommand(in Command, args);
            commands.Add(cmd);
        }
        else
        {
            cmd = commands[_count];
            Command.UnsafeSetBatchCommand(cmd);
            command.UpdateParameters(in Command, args);
        }
        _count++;
        Command.UnsafeSetBatchCommand(cmd);
    }
}
#endif
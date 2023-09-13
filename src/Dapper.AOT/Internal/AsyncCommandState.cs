using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal
{
    // split out because of async state machine semantics; see https://github.com/DapperLib/DapperAOT/issues/27
    internal class AsyncCommandState : IAsyncDisposable
    {
        private DbConnection? connection;
        public DbCommand? Command;
        private int _flags;

        const int
            FLAG_CLOSE_CONNECTION = 1 << 0,
            FLAG_PREPARE_COMMMAND = 1 << 1;
        internal void PrepareBeforeExecute() => _flags |= FLAG_PREPARE_COMMMAND;

        [MemberNotNull(nameof(Command))]
        public Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var pending = OnBeforeExecuteAsync(command, cancellationToken);
            return pending.IsCompletedSuccessfully() ? command.ExecuteScalarAsync(cancellationToken)
                : Awaited(pending, command, cancellationToken);

            static async Task<object?> Awaited(Task pending, DbCommand command, CancellationToken cancellationToken)
            {
                await pending;
                return await command.ExecuteScalarAsync(cancellationToken);
            }
        }

        [MemberNotNull(nameof(Command))]
        public Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CommandBehavior flags, CancellationToken cancellationToken)
        {
            var pending = OnBeforeExecuteAsync(command, cancellationToken);
            return pending.IsCompletedSuccessfully() ? command.ExecuteReaderAsync(flags, cancellationToken)
                : Awaited(pending, command, flags, cancellationToken);

            static async Task<DbDataReader> Awaited(Task pending, DbCommand command, CommandBehavior flags, CancellationToken cancellationToken)
            {
                await pending;
                return await command.ExecuteReaderAsync(flags, cancellationToken);
            }
        }

        [MemberNotNull(nameof(Command))]
        private Task OnBeforeExecuteAsync(DbCommand command, CancellationToken cancellationToken)
        {
            Debug.Assert(command?.Connection is not null);
            Command = command!;
            connection = command!.Connection;

            if (connection.State == ConnectionState.Open)
            {
                if ((_flags & FLAG_PREPARE_COMMMAND) == 0)
                {
                    // nothing to do
                    return Task.CompletedTask;
                }
                else
                {
                    // just need to prepare
#if NETCOREAPP3_1_OR_GREATER
                    return command.PrepareAsync(cancellationToken);
#else
                    command.Prepare();
                    return Task.CompletedTask;
#endif
                }
            }
            else
            {
                _flags |= FLAG_CLOSE_CONNECTION;
                if ((_flags & FLAG_PREPARE_COMMMAND) == 0)
                {
                    // just need to open
                    return connection.OpenAsync(cancellationToken);
                }
                else
                {
                    return OpenAndPrepareAsync(command, cancellationToken);

                    static async Task OpenAndPrepareAsync(DbCommand command, CancellationToken cancellationToken)
                    {
                        await command.Connection!.OpenAsync(cancellationToken);
#if NETCOREAPP3_1_OR_GREATER
                        await command.PrepareAsync(cancellationToken);
#else
                        command.Prepare();
#endif
                    }
                }
            }
        }


        [MemberNotNull(nameof(Command))]
        public Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
        {
            var pending = OnBeforeExecuteAsync(command, cancellationToken);
            return pending.IsCompletedSuccessfully() ? command.ExecuteNonQueryAsync(cancellationToken)
                : Awaited(pending, command, cancellationToken);

            static async Task<int> Awaited(Task pending, DbCommand command, CancellationToken cancellationToken)
            {
                await pending;
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            var cmd = Command;
            Command = null;

            var conn = connection;
            connection = null;

            if (cmd is not null)
            {
                if (conn is not null && (_flags & FLAG_CLOSE_CONNECTION) != 0)
                {
                    // need to close the connection and dispose the command
                    _flags &= ~FLAG_CLOSE_CONNECTION;
                    return DisposeCommandAndCloseConnectionAsync(conn, cmd);

#if NETCOREAPP3_1_OR_GREATER
                    static async ValueTask DisposeCommandAndCloseConnectionAsync(DbConnection conn, DbCommand cmd)
                    {
                        await cmd.DisposeAsync();
                        await conn.CloseAsync();
                    }
#else
                    static ValueTask DisposeCommandAndCloseConnectionAsync(DbConnection conn, DbCommand cmd)
                    {
                        cmd.Dispose();
                        conn.Close();
                        return default;
                    }
#endif
                }
                else
                {
                    // just need to dispose the command
#if NETCOREAPP3_1_OR_GREATER
                    return cmd.DisposeAsync();
#else
                    cmd.Dispose();
                    return default;
#endif
                }
            }
            else
            {
                if (conn is not null && (_flags & FLAG_CLOSE_CONNECTION) != 0)
                {
#if NETCOREAPP3_1_OR_GREATER
                    return new(conn.CloseAsync());
#else
                    conn.Close();
                    return default;
#endif
                }
                else
                {
                    // nothing to do
                    return default;
                }
            }
        }

        public virtual void Dispose()
        {
            var cmd = Command;
            Command = null;
            cmd?.Dispose();

            var conn = connection;
            connection = null;
            if (conn is not null && (_flags & FLAG_CLOSE_CONNECTION) != 0)
            {
                _flags &= ~FLAG_CLOSE_CONNECTION;
                conn.Close();
            }
        }
    }

}


#if !NET6_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    [Conditional("DEBUG")]
    file sealed class MemberNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with a field or property member.</summary>
        /// <param name="member">
        /// The field or property member that is promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(string member) => Members = new[] { member };

        /// <summary>Initializes the attribute with the list of field and property members.</summary>
        /// <param name="members">
        /// The list of field and property members that are promised to be not-null.
        /// </param>
        public MemberNotNullAttribute(params string[] members) => Members = members;

        /// <summary>Gets field or property member names.</summary>
        public string[] Members { get; }
    }
}

#endif
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dapper.Internal
{

    internal struct CommandState // note mutable; deliberately not : IDisposable, as that creates a *copy*
    {
        private DbConnection? connection;
        public DbCommand? Command;
        private int _flags;

        const int
            FLAG_CLOSE_CONNECTION = 1 << 0,
            FLAG_PREPARE_COMMMAND = 1 << 1;
        internal void PrepareBeforeExecute() => _flags |= FLAG_PREPARE_COMMMAND;


        [MemberNotNull(nameof(Command))]
        public object? ExecuteScalar(DbCommand command)
        {
            OnBeforeExecute(command);
            return command.ExecuteScalar();
        }

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
        public DbDataReader ExecuteReader(DbCommand command, CommandBehavior flags)
        {
            OnBeforeExecute(command);
            return command.ExecuteReader(flags);
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
        private void OnBeforeExecute(DbCommand command)
        {
            Debug.Assert(command?.Connection is not null);
            Command = command;
            connection = command.Connection;
            
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                _flags |= FLAG_CLOSE_CONNECTION;
            }
            if ((_flags & FLAG_PREPARE_COMMMAND) != 0)
            {
                _flags &= ~FLAG_PREPARE_COMMMAND;
                command.Prepare();
            }
        }

        [MemberNotNull(nameof(Command))]
        private Task OnBeforeExecuteAsync(DbCommand command, CancellationToken cancellationToken)
        {
#if NETCOREAPP3_1_OR_GREATER
            Debug.Assert(command?.Connection is not null);
            Command = command;
            connection = command.Connection;

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
                    return command.PrepareAsync(cancellationToken);
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
                        await command.PrepareAsync(cancellationToken);
                    }
                }
            }
#else
            OnBeforeExecute(command);
            return Task.CompletedTask;
#endif
        }

        [MemberNotNull(nameof(Command))]
        public int ExecuteNonQuery(DbCommand command)
        {
            OnBeforeExecute(command);
            return command.ExecuteNonQuery();
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

        public void Dispose()
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

#if NETCOREAPP3_1_OR_GREATER
        public Task DisposeAsync()
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

                    static async Task DisposeCommandAndCloseConnectionAsync(DbConnection conn, DbCommand cmd)
                    {
                        await cmd.DisposeAsync();
                        await conn.CloseAsync();
                    }
                }
                else
                {
                    // just need to dispose the command
                    return cmd.DisposeAsync().AsTask();
                }
            }
            else
            {
                if (conn is not null && (_flags & FLAG_CLOSE_CONNECTION) != 0)
                {
                    return conn.CloseAsync();
                }
                else
                {
                    // nothing to do
                    return Task.CompletedTask;
                }
            }
        }
#else
        public Task DisposeAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }
#endif
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
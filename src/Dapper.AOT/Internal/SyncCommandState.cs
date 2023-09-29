using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Dapper.Internal
{

    internal struct SyncCommandState // note mutable; deliberately not : IDisposable, as that creates a *copy*
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
        public DbDataReader ExecuteReader(DbCommand command, CommandBehavior flags)
        {
            OnBeforeExecute(command);
            return command.ExecuteReader(flags);
        }

        [MemberNotNull(nameof(Command))]
        private void OnBeforeExecute(DbCommand command)
        {
            Debug.Assert(command?.Connection is not null);
            Command = command!;
            connection = command!.Connection;
            
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
        public int ExecuteNonQuery(DbCommand command)
        {
            OnBeforeExecute(command);
            return command.ExecuteNonQuery();
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
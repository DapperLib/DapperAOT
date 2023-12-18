#if NETFRAMEWORK
using SqliteConnection = System.Data.SQLite.SQLiteConnection;
#else
using SqliteConnection = Microsoft.Data.Sqlite.SqliteConnection;
#endif

using System;
using Xunit;

namespace Dapper.AOT.Test.Integration
{
    public sealed class SqliteTests : IDisposable
    {
        private readonly SqliteConnection connection;

        public void Dispose() => connection.Dispose();

        [DapperAot(false)]
        public SqliteTests()
        {
            connection = new("Data Source=:memory:");
            connection.Open();
            connection.Execute("CREATE TABLE Foo(Id INTEGER PRIMARY KEY ASC, Name TEXT, Value INTEGER)"); // vanilla Dapper
        }

        [Fact]
        public void InsertAndSelect()
        {
            // use the generated output from SqliteUsage.Output.cs
            foreach (SomeEnum e in Enum.GetValues(typeof(SomeEnum)))
            {
                SqliteUsage.Insert(connection, e.ToString(), e);
            }

            var rows = SqliteUsage.GetAll(connection);
        }
    }
}
#if !NETFRAMEWORK
using SqliteConnection = Microsoft.Data.Sqlite.SqliteConnection;
using SqliteException = Microsoft.Data.Sqlite.SqliteException;
using System;
using System.IO;
using System.Threading.Tasks;
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

        [Fact]
        public void OmittedCommandTimeoutKeepsProviderDefault()
        {
            // an intercepted call with no commandTimeout must not override the provider default with 0 (no limit)
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var locker = new SqliteConnection($"Data Source={path};Pooling=False");
                locker.Open();
                using var writer = new SqliteConnection($"Data Source={path};Pooling=False") { DefaultTimeout = 1 };
                writer.Open();
                using (var cmd = locker.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE Foo(Id INTEGER PRIMARY KEY ASC, Name TEXT, Value INTEGER); BEGIN IMMEDIATE;";
                    cmd.ExecuteNonQuery();
                }

                // the write lock is held by the other connection, so this can only end by timing out
                var insert = Task.Run(() => SqliteUsage.Insert(writer, "a", SomeEnum.A));
                var ex = Assert.Throws<AggregateException>(() => Assert.True(insert.Wait(TimeSpan.FromSeconds(30)), "the insert should have given up after the default timeout"));
                Assert.IsType<SqliteException>(ex.InnerException);
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}
#endif
#if !NETFRAMEWORK
using SqliteConnection = Microsoft.Data.Sqlite.SqliteConnection;
using System;
using System.Collections.Generic;
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
            var expected = new List<SomeEnum>();
            foreach (SomeEnum e in Enum.GetValues(typeof(SomeEnum)))
            {
                SqliteUsage.Insert(connection, e.ToString(), e);
                expected.Add(e);
            }

            var rows = SqliteUsage.GetAll(connection);
            Assert.Equal(expected.Count, rows.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i], rows[i].Value);
                Assert.Equal(expected[i].ToString(), rows[i].Name);
            }
        }

        [Fact]
        public void SelectEnumValues()
        {
            // enum as a single-value result, via the generated row factory
            foreach (SomeEnum e in Enum.GetValues(typeof(SomeEnum)))
            {
                SqliteUsage.Insert(connection, e.ToString(), e);
            }

            var values = SqliteUsage.GetValues(connection);
            Assert.Equal(new[] { SomeEnum.A, SomeEnum.B, SomeEnum.C }, values);
        }
    }
}
#endif

using Dapper.AOT.Test;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace Dapper.Integration;

public sealed class SqlClientDatabase : IDisposable
{
    private readonly SqlConnection? connection;
    public SqlClientDatabase()
    {
        try
        {
            connection = new("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=2");
            try { connection.Execute("drop table AotIntegrationBatchTests;"); } catch { }
            connection.Execute("create table AotIntegrationBatchTests(Id int not null identity(1,1), Name nvarchar(200) not null);");
        }
        catch
        {
            // unable to guarantee working fixture
            connection?.Dispose();
            connection = null;
        }
    }
    public void Dispose() => connection?.Dispose();

    public DbConnection Connection => connection ?? SkipTest();

    private static DbConnection SkipTest()
    {
        Skip.Inconclusive("Database unavailable");
        return null!;
    }
}

public class BatchTests : IClassFixture<SqlClientDatabase>
{
    private readonly SqlClientDatabase Database;
    public BatchTests(SqlClientDatabase database) => Database = database;

    private Batch<string> Batch => Database.Connection.Batch("insert AotIntegrationBatchTests (Name) values (@name)", handler: CustomHandler.Instance);

    [SkippableTheory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BasicBatchUsage_Array(int count)
    {
        string[] data = count < 0 ? null! : Enumerable.Repeat("bar", count).ToArray();
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute(data));
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute((IEnumerable<string>)data));
    }

    [SkippableTheory]
    [InlineData(-1, 0, 0, true)]
    [InlineData(-1, -1, 0, false)]
    [InlineData(-1, 0, -1, false)]
    [InlineData(-1, 1, 0, false)]
    [InlineData(-1, 0, 1, false)]

    [InlineData(0, 0, 0, true)]
    [InlineData(0, -1, 0, false)]
    [InlineData(0, 0, -1, false)]
    [InlineData(0, 1, 0, false)]
    [InlineData(0, 0, 1, false)]
    
    [InlineData(1, 0, 0, true)]
    [InlineData(1, 1, 0, true)]
    [InlineData(1, 2, 0, false)]
    [InlineData(1, -1, 0, false)]

    [InlineData(1, 0, 1, true)]
    [InlineData(1, 1, 1, false)]

    [InlineData(2, 0, 0, true)]
    [InlineData(2, 0, 1, true)]
    [InlineData(2, 0, 2, true)]
    [InlineData(2, 1, 1, true)]
    [InlineData(2, 1, 0, true)]
    [InlineData(2, 2, 0, true)]

    [InlineData(5, 0, 5, true)]
    [InlineData(5, 1, 3, true)]
    public void BasicBatchUsage_ArraySegment(int size, int offset, int count, bool valid)
    {
        Assert.NotNull(Database.Connection);
        string[] data = size < 0 ? null! : Enumerable.Repeat("bar", size).ToArray();
        if (valid)
        {
            Assert.Equal(count, Batch.Execute(data, offset, count));
            if (data is not null)
            {
                Assert.Equal(count, Batch.Execute(new ArraySegment<string>(data, offset, count).AsSpan()));
                Assert.Equal(count, Batch.Execute(new ArraySegment<string>(data, offset, count).AsEnumerable()));
            }
        }
        else
        {
            Assert.ThrowsAny<ArgumentException>(() => Batch.Execute(data, offset, count));
            Assert.ThrowsAny<ArgumentException>(() => Batch.Execute(new ArraySegment<string>(data, offset, count).AsSpan()));
            Assert.ThrowsAny<ArgumentException>(() => Batch.Execute(new ArraySegment<string>(data, offset, count).AsEnumerable()));
        }
    }

    [SkippableTheory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BasicBatchUsage_Span(int count)
    {
        ReadOnlySpan<string> data = count < 0 ? default : Enumerable.Repeat("bar", count).ToArray();
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute(data));
    }

    [SkippableTheory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BasicBatchUsage_List(int count)
    {
        List<string> data = count < 0 ? null!: Enumerable.Repeat("bar", count).ToList();
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute(data));
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute((IEnumerable<string>)data));
    }

    [SkippableTheory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BasicBatchUsage_Collection(int count)
    {
        Collection<string> data = count < 0 ? null! : new Collection<string>(Enumerable.Repeat("bar", count).ToArray());
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute(data));
    }

    [SkippableTheory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void BasicBatchUsage_ImmutableArray(int count)
    {
        ImmutableArray<string> data = count < 0 ? default : Enumerable.Repeat("bar", count).ToImmutableArray();
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute(data));
        Assert.Equal(count < 0 ? 0 : count, Batch.Execute((IEnumerable<string>)data));
    }

    internal class CustomHandler : CommandFactory<string>
    {
        public static readonly CustomHandler Instance = new();
        private CustomHandler() { }

        public override void AddParameters(DbCommand command, string name)
        {
            var p = command.CreateParameter();
            p.ParameterName = "name";
            p.DbType = DbType.String;
            p.Size = 400;
            p.Value = AsValue(name);
            command.Parameters.Add(p);
        }

        public override void UpdateParameters(DbCommand command, string name)
        {
            command.Parameters[0].Value = AsValue(name);
        }

        public override bool CanPrepare => true;
    }
}


using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Dapper.AOT.Test.Integration;


[Collection(SharedSqlClient.Collection)]
public class BulkInsertIntegrationTests : IDisposable
{
    private readonly SqlConnection _connection;
    public BulkInsertIntegrationTests(SqlClientFixture fixture)
    {
        _connection = fixture.CreateConnection();
        _connection.Open();
        _connection.Execute("create table #target(id int not null, name nvarchar(100) not null)");
    }

    public void Dispose() => _connection?.Dispose();

    static IEnumerable<Foo> GenerateRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new Foo { Id = i, Name = $"row {i}" };
        }
    }

    static async IAsyncEnumerable<Foo> GenerateRowsAsync(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if ((i % 10) == 0) await Task.Yield();
            yield return new Foo { Id = i, Name = $"row {i}" };
        }
    }

    int Count()
    {
        // let's not use Dapper to test Dapper!
        using var db = _connection.CreateCommand();
        db.Connection = _connection;
        db.CommandText = "select count(1) from #target";
        return (int)db.ExecuteScalar();
    }
    [Fact]
    public void SyncReadSyncWrite()
    {
        _connection.Execute("truncate table #target");
        using var table = new SqlBulkCopy(_connection);
        table.EnableStreaming = true;
        table.DestinationTableName = "#target";
        table.WriteToServer(TypeAccessor.CreateDataReader(GenerateRows(1000), [nameof(Foo.Id), nameof(Foo.Name)],
            accessor: DapperTypeAccessorGeneratedInterceptors.FooAccessor));
        Assert.Equal(1000, Count());
    }

    [Fact]
    public async Task SyncReadAsyncWrite()
    {
        _connection.Execute("truncate table #target");
        using var table = new SqlBulkCopy(_connection);
        table.EnableStreaming = true;
        table.DestinationTableName = "#target";
        await table.WriteToServerAsync(TypeAccessor.CreateDataReader(GenerateRows(1000), [nameof(Foo.Id), nameof(Foo.Name)],
            accessor: DapperTypeAccessorGeneratedInterceptors.FooAccessor));
        Assert.Equal(1000, Count());
    }

    [Fact]
    public void AsyncReadSyncWrite()
    {
        _connection.Execute("truncate table #target");
        using var table = new SqlBulkCopy(_connection);
        table.EnableStreaming = true;
        table.DestinationTableName = "#target";
        table.WriteToServer(TypeAccessor.CreateDataReader(GenerateRowsAsync(1000), [nameof(Foo.Id), nameof(Foo.Name)],
            accessor: DapperTypeAccessorGeneratedInterceptors.FooAccessor));
        Assert.Equal(1000, Count());
    }

    [Fact]
    public async Task AsyncReadAsyncWrite()
    {
        _connection.Execute("truncate table #target");
        using var table = new SqlBulkCopy(_connection);
        table.EnableStreaming = true;
        table.DestinationTableName = "#target";
        await table.WriteToServerAsync(TypeAccessor.CreateDataReader(GenerateRowsAsync(1000), [nameof(Foo.Id), nameof(Foo.Name)],
            accessor: DapperTypeAccessorGeneratedInterceptors.FooAccessor));
        Assert.Equal(1000, Count());
    }

    internal class Foo
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}

// extracted and tweaked from the generator as a point-in-time snapshot
file static class DapperTypeAccessorGeneratedInterceptors
{

    public static TypeAccessor<global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo> FooAccessor => DapperCustomTypeAccessor0.Instance;
    private sealed class DapperCustomTypeAccessor0 : global::Dapper.TypeAccessor<global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo>
    {
        internal static readonly DapperCustomTypeAccessor0 Instance = new();
        public override int MemberCount => 2;
        public override int? TryIndex(string name, bool exact = false)
        {
            if (exact)
            {
                return name switch
                {
                    nameof(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo.Id) => 0,
                    nameof(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo.Name) => 1,
                    _ => base.TryIndex(name, exact)
                };
            }
            else
            {
                return NormalizedHash(name) switch
                {
                    926444256U when NormalizedEquals(name, "id") => 0,
                    2369371622U when NormalizedEquals(name, "name") => 1,
                    _ => base.TryIndex(name, exact)
                };
            }
        }
        public override string GetName(int index) => index switch
        {
            0 => nameof(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo.Id),
            1 => nameof(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo.Name),
            _ => base.GetName(index)
        };
        public override object? this[global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo obj, int index]
        {
            get => index switch
            {
                0 => obj.Id,
                1 => obj.Name,
                _ => base[obj, index]
            };
            set
            {
                switch (index)
                {
                    case 0: obj.Id = (int)value!; break;
                    case 1: obj.Name = (string)value!; break;
                    default: base[obj, index] = value; break;
                };
            }
        }
        public override bool IsNullable(int index) => index switch
        {
            1 => true,
            0 => false,
            _ => base.IsNullable(index)
        };
        public override bool IsNull(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo obj, int index) => index switch
        {
            0 => false,
            1 => obj.Name is null,
            _ => base.IsNull(obj, index)
        };
        public override global::System.Type GetType(int index) => index switch
        {
            0 => typeof(int),
            1 => typeof(string),
            _ => base.GetType(index)
        };
        public override TValue GetValue<TValue>(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo obj, int index) => index switch
        {
            0 when typeof(TValue) == typeof(int) => UnsafePun<int, TValue>(obj.Id),
            1 when typeof(TValue) == typeof(string) => UnsafePun<string, TValue>(obj.Name),
            _ => base.GetValue<TValue>(obj, index)
        };
        public override void SetValue<TValue>(global::Dapper.AOT.Test.Integration.BulkInsertIntegrationTests.Foo obj, int index, TValue value)
        {
            switch (index)
            {
                case 0 when typeof(TValue) == typeof(int):
                    obj.Id = UnsafePun<TValue, int>(value);
                    break;
                case 1 when typeof(TValue) == typeof(string):
                    obj.Name = UnsafePun<TValue, string>(value);
                    break;

            }

        }

    }

}
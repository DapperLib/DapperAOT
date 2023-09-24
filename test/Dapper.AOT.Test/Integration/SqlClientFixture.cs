using Microsoft.Data.SqlClient;
using System;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[CollectionDefinition(Collection)]
public class SharedSqlClient : ICollectionFixture<SqlClientFixture>
{
    public const string Collection = nameof(SharedSqlClient);
}

public sealed class SqlClientFixture : IDisposable
{
    private SqlConnection? connection;
    public SqlClientFixture()
    {
        try
        {
            connection = new("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=2");
            try
            {
                connection.Execute(
                    """
                    drop table AotIntegrationBatchTests;
                    drop table AotIntegrationDynamicTests;
                    """);
            }
            catch { }
            connection.Execute(
                """
                create table AotIntegrationBatchTests(Id int not null identity(1,1), Name nvarchar(200) not null);
                create table AotIntegrationDynamicTests(Id int not null identity(1,1), Name nvarchar(200) not null);
                insert AotIntegrationDynamicTests (Name) values ('Fred'), ('Barney'), ('Wilma'), ('Betty');
                """);
        }
        catch
        {
            // unable to guarantee working fixture
            connection?.Dispose();
            connection = null;
        }
    }
    public void Dispose()
    {
        connection?.Dispose();
        connection = null;
    }

    public SqlConnection Connection => connection ?? SkipTest();

    private static SqlConnection SkipTest()
    {
        throw new InvalidOperationException("Database not available");
        //Skip.Inconclusive("Database unavailable");
        //return null!;
    }
}

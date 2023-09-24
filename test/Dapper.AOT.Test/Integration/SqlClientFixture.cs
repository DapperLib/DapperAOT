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
    const string connectionString = "Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=2";
    private readonly bool canConnect;
    private readonly string? message;
    public SqlClientFixture()
    {
        try
        {
            using SqlConnection connection = new("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=2");
            connection.Open();
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
            canConnect = true;
        }
        catch (Exception ex)
        {
            // unable to guarantee working fixture
            canConnect = false;
            message = ex.Message;
        }
    }
    public void Dispose() { }

    public SqlConnection CreateConnection()
    {
        if (!canConnect) SkipTest();
        return new(connectionString);
    }

    private SqlConnection SkipTest()
    {
        throw new InvalidOperationException("Database not available: " + message);
        //Skip.Inconclusive("Database unavailable");
        //return null!;
    }
}

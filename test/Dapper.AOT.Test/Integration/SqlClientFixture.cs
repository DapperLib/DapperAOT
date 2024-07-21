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

#if NETFRAMEWORK
    const string FrameworkSuffix = "_NETFX";
#else
    const string FrameworkSuffix = "_NETCA";
#endif
    public const string AotIntegrationDynamicTests = nameof(AotIntegrationDynamicTests) + FrameworkSuffix;
    public const string AotIntegrationBatchTests = nameof(AotIntegrationBatchTests) + FrameworkSuffix;
    public SqlClientFixture()
    {
        try
        {
            using SqlConnection connection = new("Server=.;Database=AdventureWorks2022;Trusted_Connection=True;TrustServerCertificate=True;Connection Timeout=2");
            connection.Open();
            BlindTry($"drop table {AotIntegrationBatchTests};");
            BlindTry($"drop table {AotIntegrationDynamicTests};");
            BlindTry($"create table {AotIntegrationBatchTests}(Id int not null identity(1,1), Name nvarchar(200) not null);");
            BlindTry($"create table {AotIntegrationDynamicTests}(Id int not null identity(1,1), Name nvarchar(200) not null);");

            connection.Execute($"""
                truncate table {AotIntegrationDynamicTests};
                insert {AotIntegrationDynamicTests} (Name) values ('Fred'), ('Barney'), ('Wilma'), ('Betty');
                """);

            canConnect = true;

            void BlindTry(string sql)
            {
                try
                {
                    connection.Execute(sql);
                }
                catch { } // swallow
            }
        }
        catch (Exception ex)
        {
            // unable to guarantee working fixture
            canConnect = false;
            message += ex.Message + Environment.NewLine;
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

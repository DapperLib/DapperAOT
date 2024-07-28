using System;
using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public abstract class InterceptorsIntegratedTestsBase
{
    private readonly IDbConnection _dbConnection;
    
    public InterceptorsIntegratedTestsBase(PostgresqlFixture fixture)
    {
        _dbConnection = fixture.NpgsqlConnection;
        SetupDatabase(_dbConnection);
    }
    
    /// <summary>
    /// Use this to make a setup for the test. Works once for the class.
    /// Example: create a table on which you will be running tests on.
    /// </summary>
    /// <param name="dbConnection">a connection to database tests are running against</param>
    protected virtual void SetupDatabase(IDbConnection dbConnection)
    {
    }

    protected TResult ExecuteUserCode<TExecutable, TResult>()
    {
        var name = GetUserSourceCode<TExecutable>();
        
        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: [],
            references: GetTestCompilationMetadataReferences(typeof(TExecutable)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        throw new Exception();
    }
    
    string GetUserSourceCode<T>()
    {
        return typeof(T).Name;
    }
    
    MetadataReference[] GetTestCompilationMetadataReferences(Type userCodeType) =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // dotnet
        MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
        MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location), // Microsoft.Data.SqlClient
        
        MetadataReference.CreateFromFile(userCodeType.Assembly.Location), // assembly with user code
    ];
}
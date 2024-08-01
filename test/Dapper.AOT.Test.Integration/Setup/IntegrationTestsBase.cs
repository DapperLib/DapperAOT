using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper.AOT.Test.Integration.Executables;
using Dapper.AOT.Test.Integration.Executables.Recording;
using Dapper.AOT.Test.Integration.Helpers;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Dapper.AOT.Test.Integration.Setup;

public abstract class IntegrationTestsBase
{
    private const string UserCodeFileName = "UserCode.cs";
    static readonly CSharpParseOptions InterceptorSupportedParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures(new[]
        {
            new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);Dapper.AOT"),
        });

    protected readonly IDbConnection DbConnection;

    protected IntegrationTestsBase(PostgresqlFixture fixture)
    {
        DbConnection = fixture.NpgsqlConnection;
        SetupDatabase(DbConnection);
    }

    protected virtual void SetupDatabase(IDbConnection dbConnection)
    {
    }
    
    protected TResult ExecuteInterceptedUserCode<TExecutable, TResult>(IDbConnection dbConnection)
    {
        var userSourceCode = ReadUserSourceCode<TExecutable>();
        
        var inputCompilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: [ Parse(UserCodeFileName, userSourceCode) ],
            references: [
                // dotnet System.Runtime
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                
                // Dapper and Dapper.AOT
                MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
                MetadataReference.CreateFromFile(typeof(Dapper.CommandFactory).Assembly.Location), // Dapper.AOT
                
                // user code
                MetadataReference.CreateFromFile(typeof(Dapper.AOT.Test.Integration.Executables.UserCode.DbStringUsage).Assembly.Location), // Dapper.AOT.Test.Integration.Executables
                
                // dependencies
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location), // System.Data.Common
                
                // Additional stuff required by Dapper.AOT generators
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location), // System.Collections
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var interceptorRecorder = new ExceptedCodeInterceptorRecorder<TExecutable>(UserCodeFileName);
        InterceptorRecorderResolver.Register(interceptorRecorder);
        
        var generator = new DapperInterceptorGenerator(withInterceptionRecording: true);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: InterceptorSupportedParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
        
        var assembly = outputCompilation.CompileToAssembly();
        var type = assembly.GetTypes().Single(t => t.FullName == typeof(TExecutable).FullName);
        var executableInstance = Activator.CreateInstance(type);
        var mainMethod = type.GetMethod(nameof(IExecutable<TExecutable>.Execute), BindingFlags.Public | BindingFlags.Instance);
        var result = mainMethod!.Invoke(obj: executableInstance, [ dbConnection ]);

        Assert.True(interceptorRecorder.WasCalled);
        Assert.True(string.IsNullOrEmpty(interceptorRecorder.Diagnostics), userMessage: interceptorRecorder.Diagnostics);
        
        return (TResult)result!;
    }
    
    protected SyntaxTree Parse(string filename, string text)
    {
        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, InterceptorSupportedParseOptions, filename);
    }

    private static string ReadUserSourceCode<TExecutable>()
    {
        var userTypeName = typeof(TExecutable).Name;
        return File.ReadAllText(Path.Combine("UserCode", $"{userTypeName}.txt"));
    }
}
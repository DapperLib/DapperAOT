using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper.AOT.Test.Integration.Executables;
using Dapper.AOT.Test.Integration.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Dapper.AOT.Test.Integration.Setup;

public abstract class IntegrationTestsBase
{
    protected TResult ExecuteInterceptedUserCode<TExecutable, TResult>(
        IDbConnection dbConnection,
        IList<SyntaxTree> syntaxTrees)
    {
        var userSourceCode = ReadUserSourceCode<TExecutable>();
        var userSourceCodeSyntaxTree = Parse("UserCode.cs", userSourceCode);
        syntaxTrees.Add(userSourceCodeSyntaxTree);
        
        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: syntaxTrees,
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Runtime ?
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime ?
                
                MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
                MetadataReference.CreateFromFile(typeof(Dapper.CommandFactory).Assembly.Location), // Dapper.AOT
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
                
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location), // System.Data.Common
                
                MetadataReference.CreateFromFile(typeof(Dapper.AOT.Test.Integration.Executables.UserCode.DbStringUsage).Assembly.Location), // Dapper.AOT.Test.Integration.Executables 
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var assembly = compilation.CompileToAssembly();
        var type = assembly.GetTypes().Single(t => t.FullName == typeof(TExecutable).FullName);
        var executableInstance = Activator.CreateInstance(type);
        var mainMethod = type.GetMethod(nameof(IExecutable<TExecutable>.Execute), BindingFlags.Public | BindingFlags.Instance);
        var result = mainMethod!.Invoke(obj: executableInstance, [ dbConnection ]);

        return (TResult)result!;
    }
    
    protected SyntaxTree Parse(string filename, string text)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures(new[]
            {
                new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);Dapper.AOT"),
            });

        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
    }

    private static string ReadUserSourceCode<TExecutable>()
    {
        var userTypeName = typeof(TExecutable).Name;
        return File.ReadAllText(Path.Combine("UserCode", $"{userTypeName}.txt"));
    }
}
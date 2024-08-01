using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper.AOT.Test.Integration.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Dapper.AOT.Test.Integration.Setup;

public abstract class IntegrationTestsBase
{
    protected TResult ExecuteInterceptedUserCode<TExecutable, TResult>(
        IDbConnection dbConnection,
        SyntaxTree[] syntaxTrees)
    {

        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: syntaxTrees,
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Runtime ?
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime ?
                
                MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
                
                MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location), // System.Data.Common
                
                MetadataReference.CreateFromFile(typeof(Dapper.AOT.Test.Integration.Executables.UserCode.DbStringUsage).Assembly.Location), // Dapper.AOT.Test.Integration.Executables 
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var assembly = compilation.CompileToAssembly();
        var type = assembly.GetTypes().Single(t => t.FullName == "ProgramNamespace.Program");
        var mainMethod = type.GetMethod("RunCode", BindingFlags.Public | BindingFlags.Static);
        var result = mainMethod!.Invoke(obj: null, [ dbConnection ]);

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
}
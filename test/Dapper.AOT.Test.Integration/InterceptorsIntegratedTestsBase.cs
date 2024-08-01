using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Dapper.AOT.Test.Integration.Executables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Dapper.AOT.Test.Integration;

[Collection(SharedPostgresqlClient.Collection)]
public abstract class InterceptorsIntegratedTestsBase
{
    const string UserCodeDir = "UserCode";
    
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
        where TExecutable : IExecutable<TResult>
    {
        var userCodeSource = ReadUserSourceCode<TExecutable>();
        var userCodeSyntaxTree = BuildInterceptorEnabledSyntaxTree("UserCode.cs", userCodeSource);
        
        var anotherCodeSyntax = BuildInterceptorEnabledSyntaxTree("Generated.cs", """
            namespace Dapper.AOT // interceptors must be in a known namespace
            {
                file static class DapperGeneratedInterceptors
                {
                    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute("UserCode.cs", 14, 34)]
                    internal static global::System.Collections.Generic.IEnumerable<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco> Query0(
                        this global::System.Data.IDbConnection cnn,
                        string sql,
                        object? param = null,
                        global::System.Data.IDbTransaction? transaction = null, bool buffered = true, int? commandTimeout = null, global::System.Data.CommandType? commandType = null)
                    {
                        throw new global::System.Exception("this is my test");
            
                        return new System.Collections.Generic.List<global::Dapper.AOT.Test.Integration.Executables.Models.DbStringPoco>();
                    }
                }
            }
            
            namespace System.Runtime.CompilerServices
            {
                // this type is needed by the compiler to implement interceptors - it doesn't need to
                // come from the runtime itself, though
            
                [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
                sealed file class InterceptsLocationAttribute : global::System.Attribute
                {
                    public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
                    {
                        _ = path;
                        _ = lineNumber;
                        _ = columnNumber;
                    }
                }
            }                                     
        """);
        
        var inputCompilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: [ userCodeSyntaxTree, anotherCodeSyntax ],
            references: GetTestCompilationMetadataReferences(typeof(TExecutable)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        // var generator = new DapperInterceptorGenerator();
        // GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: InterceptorsEnabledParseOptions);
        // driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
        
        // var assembly = Compile(outputCompilation);
        var assembly = Compile(inputCompilation);
        
        var type = assembly.GetTypes().Single(t => t.FullName == typeof(TExecutable).FullName);
        var executableInstance = (IExecutable<TResult>)Activator.CreateInstance(type)!;

        var result = executableInstance.Execute(_dbConnection);
        return result;
    }
    
    string ReadUserSourceCode<T>()
    {
        var filePath = Path.Combine(UserCodeDir, $"{typeof(T).Name}.txt");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(message: typeof(T).Name, fileName: filePath);
        }

        return File.ReadAllText(filePath);
    }
    
    static SyntaxTree BuildInterceptorEnabledSyntaxTree(string filename, string text)
    {
        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, InterceptorsEnabledParseOptions, filename);
    }
    
    static Assembly Compile(Compilation compilation)
    {
        using var peStream = new MemoryStream();
        using var pdbstream = new MemoryStream();
        var dbg = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? DebugInformationFormat.Pdb : DebugInformationFormat.PortablePdb;
        var emitResult = compilation.Emit(peStream, pdbstream, null, null, null, new EmitOptions(false, dbg));
        if (!emitResult.Success)
        {
            TryThrowErrors(emitResult.Diagnostics);
        }

        peStream.Position = pdbstream.Position = 0;
        return Assembly.Load(peStream.ToArray(), pdbstream.ToArray());

        static void TryThrowErrors(IEnumerable<Diagnostic> items)
        {
            var errors = new List<string>();
            foreach (var item in items)
            {
                if (item.Severity == DiagnosticSeverity.Error)
                {
                    errors.Add(item.GetMessage(CultureInfo.InvariantCulture));
                }
            }

            if (errors.Count > 0)
            {
                throw new ExpressionParsingException(errors);
            }
        }
    }
    
    MetadataReference[] GetTestCompilationMetadataReferences(Type userCodeType) =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // System.Runtime ?
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime ?
        
        MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
        MetadataReference.CreateFromFile(typeof(DapperAotAttribute).Assembly.Location), // Dapper.AOT
        
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
        MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location), // System.Data.Common
        
        MetadataReference.CreateFromFile(userCodeType.Assembly.Location), // UserCode
    ];
    
    static CSharpParseOptions InterceptorsEnabledParseOptions 
        => new CSharpParseOptions(LanguageVersion.Preview).WithFeatures(new[]
        {   
            new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);Dapper.AOT"),
        });
    
    class ExpressionParsingException(IEnumerable<string> errors) : Exception(string.Join(Environment.NewLine, errors));
}
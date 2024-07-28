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
        
        var compilation = CSharpCompilation.Create(
            assemblyName: "Test.dll",
            syntaxTrees: [ userCodeSyntaxTree ],
            references: GetTestCompilationMetadataReferences(typeof(TExecutable)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var assembly = Compile(compilation);
        var type = assembly.GetTypes().Single(t => t.FullName == typeof(TExecutable).FullName);
        var executableInstance = (IExecutable<TResult>)Activator.CreateInstance(type)!;

        return executableInstance.Execute(_dbConnection);
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
        var options = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures(new[]
            {
                new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);Dapper.AOT"),
                new KeyValuePair<string, string>("Features", "InterceptorsPreview"),
                new KeyValuePair<string, string>("LangVersion", "preview"),
            });

        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
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
        // dotnet
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        
#if !NET48
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
#endif
        
        // external nugets
        MetadataReference.CreateFromFile(typeof(Dapper.SqlMapper).Assembly.Location), // Dapper
        MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location), // Microsoft.Data.SqlClient
        
        // user code
        MetadataReference.CreateFromFile(userCodeType.Assembly.Location),
    ];
    
    class ExpressionParsingException(IEnumerable<string> errors) : Exception(string.Join(Environment.NewLine, errors));
}
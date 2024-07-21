using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dapper.CodeAnalysis;
using Dapper.TestCommon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Dapper.AOT.Test.Integration.Setup;

public abstract class InterceptedCodeExecutionTestsBase : GeneratorTestBase
{
    protected readonly PostgresqlFixture Fixture;
    
    protected InterceptedCodeExecutionTestsBase(PostgresqlFixture fixture, ITestOutputHelper? log) : base(log)
    {
        Fixture = fixture;
    }
    
    protected static string PrepareSourceCodeFromFile(string inputFileName, string extension = ".cs")
    {
        var fullPath = Path.Combine("InterceptionExecutables", inputFileName + extension);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(fullPath);
        }
        
        using var sr = new StreamReader(fullPath);
        return sr.ReadToEnd();
    }
    
    protected T BuildAndExecuteInterceptedUserCode<T>(
        string userSourceCode,
        string className = "Program",
        string methodName = "ExecuteAsync")
    {
        var inputCompilation = RoslynTestHelpers.CreateCompilation("Assembly", syntaxTrees: [
            BuildInterceptorSupportedSyntaxTree(filename: "Program.cs", userSourceCode)
        ]);
        
        var diagnosticsOutputStringBuilder = new StringBuilder();
        var (compilation, generatorDriverRunResult, diagnostics, errorCount) = Execute<DapperInterceptorGenerator>(inputCompilation, diagnosticsOutputStringBuilder, initializer: g =>
        {
            g.Log += message => Log(message);
        });
        
        var results = Assert.Single(generatorDriverRunResult.Results);
        Assert.NotNull(compilation);
        Assert.True(errorCount == 0, "User code should not report errors");

        var assembly = Compile(compilation!);
        var type = assembly.GetTypes().Single(t => t.FullName == $"InterceptionExecutables.{className}");
        var mainMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(mainMethod);
        
        var result = mainMethod!.Invoke(obj: null, [ Fixture.NpgsqlConnection ]);
        Assert.NotNull(result);
        if (result is not Task<T> taskResult)
        {
            throw new XunitException($"expected execution result is '{typeof(Task<T>)}' but got {result!.GetType()}");
        }

        return taskResult.GetAwaiter().GetResult();
    }
    
    SyntaxTree BuildInterceptorSupportedSyntaxTree(string filename, string text)
    {
        var options = new CSharpParseOptions(LanguageVersion.Preview)
            .WithFeatures(new []
            {
                new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "$(InterceptorsPreviewNamespaces);ProgramNamespace;Dapper.AOT"),
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
    }

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
            throw new CompilationException(errors);
        }
    }
    
    class CompilationException : Exception
    {
        public IEnumerable<string> Errors { get; private set; }

        public CompilationException(IEnumerable<string> errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            this.Errors = errors;
        }

        public CompilationException(params string[] errors)
            : base(string.Join(Environment.NewLine, errors))
        {
            this.Errors = errors;
        }
    }
}
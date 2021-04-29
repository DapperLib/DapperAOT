using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace Dapper.AOT.Test
{
    public abstract partial class GeneratorTestBase
    {
        private readonly ITestOutputHelper? _log;
        protected GeneratorTestBase(ITestOutputHelper? log)
            => _log = log;

        [return: NotNullIfNotNull("message")]
        protected string? Log(string? message)
        {
            if (message is not null) _log?.WriteLine(message);
            return message;
        }

        // input from https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators

        protected (Compilation? Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics) Execute<T>(string source,
            StringBuilder? diagnosticsTo = null,
            [CallerMemberName] string? name = null,
            string? fileName = null,
            Action<T>? initializer = null
            ) where T : class, ISourceGenerator, new()
        {
            void Output(string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _log?.WriteLine(message);
                    diagnosticsTo?.Append("// ").AppendLine(message);
                }
            }
            // Create the 'input' compilation that the generator will act on
            if (string.IsNullOrWhiteSpace(name)) name = "compilation";
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "input.cs";
            Compilation inputCompilation = CreateCompilation(source, name, fileName);

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            T generator = new();
            initializer?.Invoke(generator);
#pragma warning disable CS0618 // Type or member is obsolete
            if (_log is not null && generator is ILoggingAnalyzer logging)
            {
                logging.Log += s => Log(s);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            ShowDiagnostics("Input code", inputCompilation, diagnosticsTo, "CS8795", "CS1701", "CS1702");
            
            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
            var runResult = driver.GetRunResult();
            foreach (var result in runResult.Results)
            {
                if (result.Exception is not null) throw result.Exception;
            }

            var dn = Normalize(diagnostics, Array.Empty<string>());
            if (dn.Any())
            {
                Output($"Generator produced {dn.Count} diagnostics:");
                foreach (var d in dn)
                {
                    Output(d);
                }
            }

            ShowDiagnostics("Output code", outputCompilation, diagnosticsTo, "CS1701", "CS1702");
            return (outputCompilation, runResult, diagnostics);
        }

        void ShowDiagnostics(string caption, Compilation compilation, StringBuilder? diagnosticsTo, params string[] ignore)
        {
            if (_log is null && diagnosticsTo is null) return; // nothing useful to do!
            void Output(string message)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    _log?.WriteLine(message);
                    diagnosticsTo?.Append("// ").AppendLine(message);
                }
            }
            foreach (var tree in compilation.SyntaxTrees)
            {
                var diagnostics = Normalize(compilation.GetSemanticModel(tree).GetDiagnostics(), ignore);
                
                if (diagnostics.Any())
                {
                    Output($"{caption} has {diagnostics.Count} diagnostics from '{tree.FilePath}':");
                    foreach (var d in diagnostics)
                    {
                        Output(d);
                    }
                }
            }
        }

        static List<string> Normalize(ImmutableArray<Diagnostic> diagnostics, string[] ignore) => (
            from d in diagnostics
            where !ignore.Contains(d.Id)
            let loc = d.Location
            let msg = d.ToString()
            orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, msg
            select msg).ToList();

        protected static Compilation CreateCompilation(string source, string name, string fileName)
           => CSharpCompilation.Create(name,
               syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source).WithFilePath(fileName) },
               references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Data").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                   MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(OracleConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(CommandAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlMapper).Assembly.Location),
               },
               options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

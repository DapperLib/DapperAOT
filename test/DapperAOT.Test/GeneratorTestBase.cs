using Dapper;
using DapperAOT.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace DapperAOT.Test
{
    public abstract class GeneratorTestBase
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

        [return: NotNullIfNotNull("message")]
        protected string? Log(SourceText? message)
            => Log(message?.ToString());

        // input from https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators

        protected (Compilation? Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics) Execute<T>(string source) where T : class, ISourceGenerator, new()
        {
            // Create the 'input' compilation that the generator will act on
            Compilation inputCompilation = CreateCompilation(source);

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            T generator = new();
#pragma warning disable CS0618 // Type or member is obsolete
            if (_log is not null && generator is ILoggingAnalyzer logging)
            {
                logging.Log += s => Log(s);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            ImmutableArray<Diagnostic> diagnostics;
            if (_log is not null) // useful for finding problems in the input
            {
                foreach (var tree in inputCompilation.SyntaxTrees)
                {
                    diagnostics = inputCompilation.GetSemanticModel(tree).GetDiagnostics();
                    if (!diagnostics.IsDefaultOrEmpty)
                    {
                        int count = diagnostics.Count(d => d.Id != "CS8795");
                        if (count > 0)
                        {
                            Log($"Input code has {count} diagnostics from '{tree.FilePath}':");
                            foreach (var d in diagnostics)
                            {
                                if (d.Id != "CS8795") // partial without implementation; we know - that's what we're here to fix
                                {
                                    Log(d.ToString());
                                }
                            }
                        }
                    }
                }
            }

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out diagnostics);

            if (_log is not null)
            {
                if (!diagnostics.IsDefaultOrEmpty)
                {
                    Log($"Generator produced {diagnostics.Length} diagnostics:");
                    foreach (var d in diagnostics)
                    {
                        Log(d.ToString());
                    }
                }

                foreach (var tree in outputCompilation.SyntaxTrees)
                {
                    var tmp = outputCompilation.GetSemanticModel(tree).GetDiagnostics();
                    if (!tmp.IsDefaultOrEmpty)
                    {
                        Log($"Output code has {tmp.Length} diagnostics from '{tree.FilePath}':");
                        foreach (var d in tmp)
                        {
                            Log(d.ToString());
                        }
                    }
                }

            }

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            return (outputCompilation, runResult, diagnostics);
        }
        protected static Compilation CreateCompilation(string source)
           => CSharpCompilation.Create("compilation",
               syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source).WithFilePath("input.cs") },
               references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                   MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(CommandAttribute).Assembly.Location)
               },
               options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

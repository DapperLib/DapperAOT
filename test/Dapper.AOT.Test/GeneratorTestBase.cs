using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Dapper.AOT.Test
{
    public abstract partial class GeneratorTestBase
    {
        private readonly ITestOutputHelper? _log;
        protected GeneratorTestBase(ITestOutputHelper? log)
            => _log = log;

#if !NET48
        [return: NotNullIfNotNull(nameof(message))]
#endif
        protected string? Log(string? message)
        {
            if (message is not null) _log?.WriteLine(message);
            return message;
        }

        protected static string? GetOriginCodeLocation([CallerFilePath] string? path = null) => path;

        // input from https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators

        protected (Compilation? Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics, int ErrorCount) Execute<T>(string source,
            StringBuilder? diagnosticsTo = null,
            [CallerMemberName] string? name = null,
            string? fileName = null,
            Action<T>? initializer = null
            ) where T : class, new()
        {
            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line} C{loc.StartLinePosition.Character}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
            void Output(string message, bool force = false)
            {
                if (force || !string.IsNullOrWhiteSpace(message))
                {
                    _log?.WriteLine(message);
                    diagnosticsTo?.AppendLine(message.Replace('\\', '/')); // need to normalize paths
                }
            }
            // Create the 'input' compilation that the generator will act on
            if (string.IsNullOrWhiteSpace(name)) name = "compilation";
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "input.cs";
            Compilation inputCompilation = CreateCompilation(source, name!, fileName!);

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            T generator = new();
            initializer?.Invoke(generator);

            ShowDiagnostics("Input code", inputCompilation, diagnosticsTo, "CS8795", "CS1701", "CS1702");

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = generator switch
            {
                IIncrementalGenerator incremental => CSharpGeneratorDriver.Create(incremental),
                ISourceGenerator basic => CSharpGeneratorDriver.Create(basic),
                _ => throw new ArgumentException("Generator must implement IIncrementalGenerator or ISourceGenerator"),
            };

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
                    OutputDiagnostic(d);
                }
            }

            var errorCount = ShowDiagnostics("Output code", outputCompilation, diagnosticsTo, "CS1701", "CS1702");
            return (outputCompilation, runResult, diagnostics, errorCount);
        }

        int ShowDiagnostics(string caption, Compilation compilation, StringBuilder? diagnosticsTo, params string[] ignore)
        {
            if (_log is null && diagnosticsTo is null) return 0; // nothing useful to do!
            void Output(string message, bool force = false)
            {
                if (force || !string.IsNullOrWhiteSpace(message))
                {
                    _log?.WriteLine(message);
                    diagnosticsTo?.AppendLine(message.Replace('\\', '/')); // need to normalize paths
                }
            }
            int errorCountTotal = 0;
            foreach (var tree in compilation.SyntaxTrees)
            {
                var rawDiagnostics = compilation.GetSemanticModel(tree).GetDiagnostics();
                var diagnostics = Normalize(rawDiagnostics, ignore);
                errorCountTotal += rawDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);

                if (diagnostics.Any())
                {
                    Output($"{caption} has {diagnostics.Count} diagnostics from '{tree.FilePath}':");
                    foreach (var d in diagnostics)
                    {
                        OutputDiagnostic(d);
                    }
                }
            }
            return errorCountTotal;

            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line} C{loc.StartLinePosition.Character}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        static List<Diagnostic> Normalize(ImmutableArray<Diagnostic> diagnostics, string[] ignore) => (
            from d in diagnostics
            where !ignore.Contains(d.Id)
            let loc = d.Location
            orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, d.ToString()
            select d).ToList();

        static readonly CSharpParseOptions ParseOptionsLatestLangVer = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

        protected static Compilation CreateCompilation(string source, string name, string fileName)
           => CSharpCompilation.Create(name,
               syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, ParseOptionsLatestLangVer).WithFilePath(fileName) },
               references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
#if !NET48
                   MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Data").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
#endif
                   MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(System.Data.SqlClient.SqlConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(OracleConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ValueTask<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Command<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlMapper).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableList<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableArray<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Span<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IgnoreDataMemberAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlMapper).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location),
               },
               options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true));
    }
}

using Dapper.TestCommon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
            ) where T : class, IIncrementalGenerator, new()
        {
            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}");
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
            Compilation inputCompilation = RoslynTestHelpers.CreateCompilation(source, name!, fileName!);

            // directly create an instance of the generator
            // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
            T generator = new();
            initializer?.Invoke(generator);

            ShowDiagnostics("Input code", inputCompilation, diagnosticsTo, "CS8795", "CS1701", "CS1702");

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: RoslynTestHelpers.ParseOptionsLatestLangVer);

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
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        static List<Diagnostic> Normalize(ImmutableArray<Diagnostic> diagnostics, string[] ignore) => (
            from d in diagnostics
            where !ignore.Contains(d.Id)
            let loc = d.Location
            orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, d.ToString()
            select d).ToList();
    }
}

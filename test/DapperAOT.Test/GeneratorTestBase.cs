using Dapper;
using DapperAOT.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

            // Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Run the generation pass
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            return (outputCompilation, runResult, diagnostics);
        }
        protected static Compilation CreateCompilation(string source)
           => CSharpCompilation.Create("compilation",
               syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
               references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(CommandAttribute).Assembly.Location)
               },
               options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}

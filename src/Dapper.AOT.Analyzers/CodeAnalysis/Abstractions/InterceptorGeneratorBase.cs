using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dapper.CodeAnalysis.Abstractions
{
    public abstract class InterceptorGeneratorBase : DiagnosticAnalyzer, IIncrementalGenerator,
        ISourceGenerator // for CSharpGeneratorDriver - see Roslyn #69906
    {
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // we won't register anything; all we really want here is to report our supported diagnostics
        }

        /// <summary>
        /// Whether to emit interceptors even if the "interceptors" feature is not detected
        /// </summary>
        public bool OverrideFeatureEnabled { get; set; }

        /// <inheritdoc/>
        public abstract void Initialize(IncrementalGeneratorInitializationContext context);

        // for CSharpGeneratorDriver - see Roslyn #69906
        void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }
        // for CSharpGeneratorDriver - see Roslyn #69906
        void ISourceGenerator.Execute(GeneratorExecutionContext context) { }
    }
}

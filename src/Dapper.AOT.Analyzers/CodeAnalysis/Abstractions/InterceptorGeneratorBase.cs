using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dapper.CodeAnalysis.Abstractions
{
    public abstract class InterceptorGeneratorBase : DiagnosticAnalyzer, IIncrementalGenerator
    {
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            // we won't register anything; all we really want here is to report our supported diagnostics
        }

        /// <inheritdoc/>
        public abstract void Initialize(IncrementalGeneratorInitializationContext context);
    }
}

using Dapper.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;

namespace Dapper.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OpinionatedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.All;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // respond to method usages
        context.RegisterOperationAction(OnOperation, OperationKind.Invocation);
    }

    private void OnOperation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation op)
        {
            return; // not our concern
        }
        var methodKind = op.IsSupportedDapperMethod(out var flags);
        switch (methodKind)
        {
            case DapperMethodKind.DapperUnsupported:
                flags |= OperationFlags.DoNotGenerate;
                break;
            case DapperMethodKind.DapperSupported:
                break;
            default: // includes NotDapper
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.DapperAotNotEnabled, null));

    }
}

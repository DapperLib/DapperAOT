﻿using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using static Dapper.CodeAnalysis.DapperInterceptorGenerator;

namespace Dapper.AOT.Test.TestCommon;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class WrappedDapperInterceptorAnalyzer : DiagnosticAnalyzer
{
    private readonly DapperInterceptorGenerator inner = new();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => inner.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var state = new GenerationState(inner);
        context.RegisterOperationAction(state.OnOperation, OperationKind.Invocation);
        context.RegisterCompilationEndAction(state.OnCompilationEnd);
    }

    private class GenerationState
    {
        public GenerationState(DapperInterceptorGenerator inner)
        {
            this.inner = inner;
        }
        private readonly DapperInterceptorGenerator inner;

        private readonly ConcurrentBag<SourceState> _bag = new();
        public void OnCompilationEnd(CompilationAnalysisContext context)
        => inner.Generate(new GenerateState(GenerateContextProxy.Create(context, _bag.ToImmutableArray())));


        public void OnOperation(OperationAnalysisContext context)
        {
            if (!inner.PreFilter(context.Operation.Syntax, context.CancellationToken)) return;
            var state = new ParseState(ParseContextProxy.Create(context));
            var parsed = inner.Parse(state);
            if (parsed is not null)
            {
                _bag.Add(parsed);
            }
        }
    }

    
}

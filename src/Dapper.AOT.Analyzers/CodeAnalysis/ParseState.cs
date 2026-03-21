using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using static Dapper.CodeAnalysis.DapperInterceptorGenerator;

namespace Dapper.CodeAnalysis;

//SourceProductionContext ctx, (Compilation Compilation, ImmutableArray < SourceState > Nodes
internal readonly struct ParseState
{
    public ParseState(ParseContextProxy proxy)
    {
        CancellationToken = proxy.CancellationToken;
        Node = proxy.Node;
        SemanticModel = proxy.SemanticModel;
        Options = proxy.Options;
    }
    public ParseState(in GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
        Node = context.Node;
        SemanticModel = context.SemanticModel;
        Options = null;
    }
    public ParseState(in OperationAnalysisContext context)
    {
        CancellationToken = context.CancellationToken;
        Node = context.Operation.Syntax;
        SemanticModel = context.Operation.SemanticModel!;
        Options = context.Options;
    }
    public readonly CancellationToken CancellationToken;
    public readonly SyntaxNode Node;
    public readonly SemanticModel SemanticModel;
    public readonly AnalyzerOptions? Options;
}

internal abstract class ParseContextProxy
{
    public abstract SyntaxNode Node { get; }
    public abstract CancellationToken CancellationToken { get; }
    public abstract SemanticModel SemanticModel { get; }
    public abstract AnalyzerOptions? Options { get; }

    //public static ParseContextProxy Create(in OperationAnalysisContext context)
    //    => new OperationAnalysisContextProxy(in context);

    //private sealed class OperationAnalysisContextProxy : ParseContextProxy
    //{
    //    private readonly OperationAnalysisContext context;
    //    public OperationAnalysisContextProxy(in OperationAnalysisContext context)
    //        => this.context = context;
    //    public override SyntaxNode Node => context.Operation.Syntax;
    //    public override CancellationToken CancellationToken => context.CancellationToken;
    //    public override SemanticModel SemanticModel => context.Compilation.GetSemanticModel(Node.SyntaxTree);
    //    public override AnalyzerOptions? Options => context.Options;
    //}
}


internal readonly struct GenerateState
{
    public GenerateState(GenerateContextProxy proxy)
    {
        Compilation = proxy.Compilation;
        Nodes = proxy.Nodes;
        ctx = default;
        this.proxy = proxy;
    }
    public GenerateState(SourceProductionContext ctx, in (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        Compilation = state.Compilation;
        Nodes = state.Nodes;
        this.ctx = ctx;
        proxy = null;
    }
    private readonly SourceProductionContext ctx;
    private readonly GenerateContextProxy? proxy;
    public readonly ImmutableArray<SourceState> Nodes;
    public readonly Compilation Compilation;
    public readonly GeneratorContext GeneratorContext = new();

    internal void ReportDiagnostic(Diagnostic diagnostic)
    {
        if (proxy is not null)
        {
            proxy.ReportDiagnostic(diagnostic);
        }
        else
        {
            ctx.ReportDiagnostic(diagnostic);
        }
    }

    internal void AddSource(string hintName, string text)
    {
        if (proxy is not null)
        {
            proxy.AddSource(hintName, text);
        }
        else
        {
            ctx.AddSource(hintName, SourceText.From(text, Encoding.UTF8));
        }
    }

    // see https://github.com/dotnet/roslyn/blob/main/docs/features/interceptors.md#file-paths
    internal string GetInterceptorFilePath(SyntaxTree? tree)
    {
        if (tree is null) return "";
        return Compilation.Options.SourceReferenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
    }
}


internal abstract class GenerateContextProxy
{
    public abstract Compilation Compilation { get; }
    public abstract ImmutableArray<SourceState> Nodes { get; }

    public static GenerateContextProxy Create(in CompilationAnalysisContext context, ImmutableArray<SourceState> nodes)
        => new CompilationAnalysisContextProxy(in context, nodes);

    internal virtual void AddSource(string hintName, string text) { }
    internal virtual void ReportDiagnostic(Diagnostic diagnostic) { }

    private sealed class CompilationAnalysisContextProxy : GenerateContextProxy
    {
        private readonly CompilationAnalysisContext context;
        private readonly ImmutableArray<SourceState> nodes;
        public CompilationAnalysisContextProxy(in CompilationAnalysisContext context, ImmutableArray<SourceState> nodes)
        {
            this.context = context;
            this.nodes = nodes;
        }

        internal override void ReportDiagnostic(Diagnostic diagnostic)
            => context.ReportDiagnostic(diagnostic);
        public override Compilation Compilation => context.Compilation;
        public override ImmutableArray<SourceState> Nodes => nodes;
    }
}

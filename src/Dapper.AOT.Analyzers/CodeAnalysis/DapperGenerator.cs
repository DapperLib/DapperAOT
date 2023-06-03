using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dapper.CodeAnalysis;

/// <summary>
/// Analyzes source for Dapper syntax and generates suitable interceptors where possible.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DapperGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Provide log feedback.
    /// </summary>
    public event Action<DiagnosticSeverity, string>? Log;

    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                    .Where(x => x is not null)
                    .Select((x, _) => x!);
        var combined = context.CompilationProvider.Combine(nodes.Collect());
        context.RegisterImplementationSourceOutput(combined, Generate);
    }

    private bool PreFilter(SyntaxNode node, CancellationToken cancellation)
    {
        if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            var name = ma.Name.ToString();
            if (name is "Execute" or "ExecuteAsync")
            {
                Log?.Invoke(DiagnosticSeverity.Info, $"Discovered possible execution: {node}");
                return true;
            }
            if (name.StartsWith("Query<") || name.StartsWith("QueryAsync<"))
            {
                Log?.Invoke(DiagnosticSeverity.Info, $"Discovered possible <T> query: {node}");
                return true;
            }
        }

        return false;
    }

    private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellation)
    {
        if (ctx.Node is not InvocationExpressionSyntax ie
            || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op
            || !IsSupportedDapperMethod(op, out var flags))
        {
            return null;
        }
        Location? loc = null;
        if (op.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            loc = ma.ChildNodes().Skip(1).FirstOrDefault()?.GetLocation();
        }
        loc ??= op.Syntax.GetLocation();
        if (loc is null)
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"No location found; cannot intercept");
            return null;
        }
        Log?.Invoke(DiagnosticSeverity.Hidden, $"Found {op.TargetMethod.Name}: {flags} at {loc}");

        ITypeSymbol? resultType = null, paramType = null;
        if (HasAny(flags, OperationFlags.TypedResult))
        {
            var typeArgs = op.TargetMethod.TypeArguments;
            if (typeArgs.Length == 1)
            {
                resultType = typeArgs[0];
            }
            if (resultType is null)
            {
                Log?.Invoke(DiagnosticSeverity.Hidden, $"Unable to resolve data type");
                return null;
            }
        }

        string? sql = null;
        bool? buffered = null;
        foreach (var arg in op.Arguments)
        {
            switch (arg.Parameter?.Name)
            {
                case "sql":
                    if (!TryGetConstantValue(arg, out string? s))
                    {
                        Log?.Invoke(DiagnosticSeverity.Hidden, $"Non-constant value for '{arg.Parameter?.Name}'");
                        return null;
                    }
                    sql = s;
                    break;
                case "buffered":
                    if (!TryGetConstantValue(arg, out bool b))
                    {
                        Log?.Invoke(DiagnosticSeverity.Hidden, $"Non-constant value for '{arg.Parameter?.Name}'");
                        return null;
                    }
                    buffered = b;
                    break;
                case "param":
                    if (arg.Value is not IDefaultValueOperation)
                    {
                        var expr = arg.Value;
                        if (expr is IConversionOperation conv && IsObject(expr.Type))
                        {
                            expr = conv.Operand;
                        }
                        paramType = expr?.Type;
                        if (paramType is null || IsObject(paramType))
                        {
                            Log?.Invoke(DiagnosticSeverity.Hidden, $"Cannot use parameter type '{paramType?.Name}'");
                            return null;
                        }
                        flags |= OperationFlags.HasParameters;
                    }
                    break;
                case "cnn":
                case "commandTimeout":
                case "commandType":
                case "transaction":
                    // nothing to do
                    break;
                default:
                    Log?.Invoke(DiagnosticSeverity.Hidden, $"Unexpected parameter '{arg.Parameter?.Name}'");
                    return null;
            }
        }
        if (string.IsNullOrWhiteSpace(sql))
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"SQL not detected");
            return null;
        }
        if (HasAny(flags, OperationFlags.Query) && (buffered ?? true))
        {
            flags |= OperationFlags.Buffered;
        }
        Log?.Invoke(DiagnosticSeverity.Hidden, $"OK, {flags}, result-type: {resultType}, param-type: {paramType}");

        return new SourceState(loc, op.TargetMethod, flags, sql, resultType, paramType);

        static bool IsObject(ITypeSymbol? symbol)
            => symbol is { Name: "Object", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };

        static bool TryGetConstantValue<T>(IArgumentOperation op, out T? value)
        {
            try
            {
                if (op.ConstantValue.HasValue)
                {
                    value = (T?)op.ConstantValue.Value;
                    return true;
                }
                else if (op.Value is ILiteralOperation { ConstantValue.HasValue: true } literal)
                {
                    value = (T?)literal.ConstantValue.Value;
                    return true;
                }
            }
            catch { }
            value = default!;
            return false;
        }

        static bool IsSupportedDapperMethod(IInvocationOperation operation, out OperationFlags flags)
        {
            flags = OperationFlags.None;
            var method = operation?.TargetMethod;
            if (method is null || !method.IsExtensionMethod)
            {
                return false;
            }
            var type = method.ContainingType;
            if (type is not { Name: "SqlMapper", ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
            {
                return false;
            }
            if (method.Name.EndsWith("Async"))
            {
                flags |= OperationFlags.Async;
            }
            if (method.IsGenericMethod)
            {
                flags |= OperationFlags.TypedResult;
            }
            switch (method.Name)
            {
                case "Query":
                case "QueryAsync":
                    flags |= OperationFlags.Query;
                    return method.Arity == 1;
                case "Execute":
                case "ExecuteAsync":
                    flags |= OperationFlags.Execute;
                    return method.Arity == 0;
                default:
                    return false;
            }
        }
    }

    static bool HasAny(OperationFlags value, OperationFlags testFor)
        => (value & testFor) != 0;
    [Flags]
    enum OperationFlags
    {
        None = 0,
        Query = 1 << 0,
        Execute = 1 << 1,
        Async = 1 << 2,
        TypedResult = 1 << 3,
        HasParameters = 1 << 4,
        Buffered = 1 << 5,
    }

    private void Generate(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        if (state.Nodes.IsDefaultOrEmpty)
        {
            // nothing to generate
            return;
        }
        var sb = new StringBuilder().AppendLine("file static class DapperGeneratedInterceptors").AppendLine("{");
        int methodIndex = 0;
        foreach (var grp in state.Nodes.GroupBy(x => x.Group(), CommonComparer.Instance))
        {
            int usageCount = 0;
            foreach (var op in grp.OrderBy(row => row.Location, CommonComparer.Instance))
            {
                var loc = op.Location.GetLineSpan();
                var start = loc.StartLinePosition;
                sb.AppendLine($"    [global::System.Runtime.CompilerServices.InterceptsLocationAttribute({Verbatim(loc.Path)}, {start.Line + 1}, {start.Character + 1})]");
                usageCount++;
            }

            if (usageCount == 0)
            {
                continue; // empty group?
            }

            var (flags, method, parameterType) = grp.Key;
            sb.Append($"    internal static {method.ReturnType} {method.Name}{methodIndex++}(");
            var parameters = method.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                else if (method.IsExtensionMethod) sb.Append("this ");
                sb.Append(parameters[i].Type).Append(" ").Append(parameters[i].Name);
            }
            sb.AppendLine(")").AppendLine("    {");
            sb.AppendLine($"        // {flags}");
            if (HasAny(flags, OperationFlags.HasParameters))
            {
                sb.AppendLine($"        // takes parameter: {parameterType}");
            }
            if (HasAny(flags, OperationFlags.TypedResult))
            {
                sb.AppendLine($"        // returns data: {grp.First().ResultType}");
            }
            sb.AppendLine("        throw new global::System.NotImplementedException(\"lower your expectations\");").AppendLine("    }").AppendLine();
        }
        sb.AppendLine("}");
        ctx.AddSource($"{state.Compilation.AssemblyName ?? "package"}.generated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

        static string Verbatim(string value) => value is null ? "null"
            : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value)).ToFullString();
    }

    sealed class SourceState
    {
        public Location Location { get; }
        public OperationFlags Flags { get; }
        public string? Sql { get; }
        public IMethodSymbol Method { get; }
        public ITypeSymbol? ResultType { get; }
        public ITypeSymbol? ParameterType { get; }
        public SourceState(Location location, IMethodSymbol method, OperationFlags flags, string? sql, ITypeSymbol? resultType, ITypeSymbol? parameterType)
        {
            Location = location;
            Flags = flags;
            Sql = sql;
            ResultType = resultType;
            ParameterType = parameterType;
            Method = method;
        }

        public (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType) Group() => new(Flags, Method, ParameterType);

        
    }
    private sealed class CommonComparer : IEqualityComparer<(OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType)>, IComparer<Location>
    {
        public static readonly CommonComparer Instance = new();
        private CommonComparer() { }

        public int Compare(Location x, Location y)
        {
            if (x is null) { return y is null ? 0 : 1; }
            if (y is null) return -1;
            var xSpan = x.GetLineSpan();
            var ySpan = y.GetLineSpan();
            var delta = StringComparer.InvariantCulture.Compare(xSpan.Path, ySpan.Path);
            if (delta == 0)
            {
                delta = xSpan.StartLinePosition.CompareTo(ySpan.StartLinePosition);
            }
            if (delta == 0)
            {
                delta = xSpan.EndLinePosition.CompareTo(ySpan.EndLinePosition);
            }
            return delta;
        }

        public bool Equals((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType) x, (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType) y) => x.Flags == y.Flags
                && SymbolEqualityComparer.Default.Equals(x.Method, y.Method)
                && SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType);

        public int GetHashCode((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType) obj)
        {
            var hash = (int)obj.Flags;
            hash *= -47;
            hash += SymbolEqualityComparer.Default.GetHashCode(obj.Method);
            hash *= -47;
            if (obj.ParameterType is not null)
            {
                hash += SymbolEqualityComparer.Default.GetHashCode(obj.ParameterType);
            }
            return hash;
        }
    }
}

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using Dapper.CodeAnalysis.Abstractions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using Dapper.Internal;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;

namespace Dapper.CodeAnalysis
{
    /// <summary>
    /// Analyses source for Dapper syntax and generates suitable interceptors where possible.
    /// </summary>
    [Generator(LanguageNames.CSharp), DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypeAccessorInterceptorGenerator : InterceptorGeneratorBase
    {
        /// <summary>
        /// Provide log feedback.
        /// </summary>
        public event Action<DiagnosticSeverity, string>? Log;

        /// <inheritdoc/>
        public override void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                        .Where(x => x is not null)
                        .Select((x, _) => x!);
            var combined = context.CompilationProvider.Combine(nodes.Collect());
            context.RegisterImplementationSourceOutput(combined, Generate);
        }

        private bool PreFilter(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is InvocationExpressionSyntax invocation && invocation.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Expression.ToString() == "TypeAccessor" && memberAccess.Name.ToString() == "CreateReader";
            }

            return false;
        }

        private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            if (ctx.Node is not InvocationExpressionSyntax ie || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op)
            {
                return null;
            }
            if (!TryParseLocation(out var loc))
            {
                Log?.Invoke(DiagnosticSeverity.Hidden, $"No location found; cannot intercept");
                return null;
            }
            if (!TryParseParameterType(out var parameterType))
            {
                Log?.Invoke(DiagnosticSeverity.Hidden, $"Failed to parse parameterType; cannot intercept");
                return null;
            }

            return new SourceState(loc!, parameterType!);

            bool TryParseParameterType(out ITypeSymbol? type)
            {
                var arg = op.Arguments.FirstOrDefault(x => x.Parameter?.Name == "obj");
                if (arg is null)
                {
                    type = null;
                    return false;
                }

                if (arg.Value is not IDefaultValueOperation)
                {
                    var expr = arg.Value;
                    if (expr is IConversionOperation conv && expr.Type?.SpecialType == SpecialType.System_Object)
                    {
                        expr = conv.Operand;
                    }
                    type = expr?.Type;
                    return true;
                }

                type = null;
                return false;
            }

            bool TryParseLocation(out Location? loc)
            {
                loc = null;
                if (op.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
                {
                    loc = ma.ChildNodes().Skip(1).FirstOrDefault()?.GetLocation();
                }
                loc ??= op.Syntax.GetLocation();
                return loc is not null;
            }
        }

        private void Generate(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
        {
            if (!IsGenerateInputValid(ref ctx, state))
            {
                Log?.Invoke(DiagnosticSeverity.Hidden, $"Generate input for '{nameof(TypeAccessorInterceptorGenerator)}' does not allow generation.");
                return;
            }

            var sb = new TypeAccessorInterceptorCodeWriter();
            sb.WriteFileHeader(state.Compilation);
            sb.WriteClass(() =>
            {
                int typeCounter = 0;
                foreach (var group in state.Nodes.GroupBy(x => x, SourceStateByTypeComparer.Instance))
                {
                    var typeSymbol = group.Key.ParameterType;
                    var usages = group.Select(x => x.Location);

                    foreach (var location in usages)
                    {
                        sb.WriteInterceptorsLocationAttribute(location);
                        sb.WriteTypeAccessorCreateReaderMethod(typeCounter);
                    }

                    // Inspection.IsCollectionType(typeSymbol, out var elementType);

                    sb.WriteCustomTypeAccessorClass(typeCounter, () =>
                    {

                    });
                }
            });

            ctx.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", sb.GetSourceText());
        }

        private bool IsGenerateInputValid(ref SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
        {
            if (state.Nodes.IsDefaultOrEmpty)
            {
                // TODO report diagnostics
                return false;
            }

            return true;
        }

        sealed class SourceState
        {
            public Location Location { get; }
            public ITypeSymbol ParameterType { get; }

            public SourceState(
                Location location,
                ITypeSymbol parameterType)
            {
                Location = location;
                ParameterType = parameterType;
            }

            public (ITypeSymbol ParameterType, Location? UniqueLocation) Group()
                => new(ParameterType, Location);
        }

        [DebuggerDisplay("code: '{_sb.ToString()}'")]
        sealed class TypeAccessorInterceptorCodeWriter
        {
            readonly CodeWriter _sb = new();

            public void WriteFileHeader(Compilation compilation)
            {
                bool allowUnsafe = compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;
                if (allowUnsafe)
                {
                    _sb.Append("#nullable enable").NewLine();
                }
            }

            public void WriteClass(Action innerWriter)
            {
                _sb.Append("file static class DapperTypeAccessorGeneratedInterceptors").Indent().NewLine();
                innerWriter();
                _sb.Outdent();
            }

            public void WriteInterceptorsLocationAttribute(Location location)
            {
                var loc = location.GetLineSpan();
                var start = loc.StartLinePosition;
                _sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                    .AppendVerbatimLiteral(loc.Path).Append(", ").Append(start.Line + 1).Append(", ").Append(start.Character + 1).Append(")]")
                    .NewLine();
            }

            public void WriteTypeAccessorCreateReaderMethod(int customTypeNum)
            {
                _sb.Append("public static ObjectAccessor<T> CreateReader<T>(T obj, [DapperAot] TypeAccessor<T>? accessor = null)")
                   .Indent().NewLine()
                       .Append("return ").Append($"{GetCustomTypeAccessorClassName(customTypeNum)}.Instance").Append(";")
                   .Outdent().NewLine().NewLine();
            }

            public void WriteCustomTypeAccessorClass(int customTypeNum, Action innerWriter)
            {
                var className = GetCustomTypeAccessorClassName(customTypeNum);

                _sb.Append("private sealed class " + className)
                   .Indent().NewLine()
                   .Append($"internal static readonly {className} Instance = new();")
                   .NewLine();
                innerWriter();
                _sb.Outdent().NewLine();
            }

            public SourceText GetSourceText() => SourceText.From(_sb.ToString(), Encoding.UTF8);

            private string GetCustomTypeAccessorClassName(int num) => "DapperCustomTypeAccessor" + num;
        }

        sealed class SourceStateByTypeComparer : IEqualityComparer<SourceState>
        {
            public static readonly SourceStateByTypeComparer Instance = new();

            public bool Equals(SourceState x, SourceState y) => SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType);
            public int GetHashCode(SourceState obj) => SymbolEqualityComparer.Default.GetHashCode(obj.ParameterType);
        }
    }
}

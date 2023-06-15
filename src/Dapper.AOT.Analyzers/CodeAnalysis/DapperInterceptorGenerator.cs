using Dapper.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dapper.CodeAnalysis;

/// <summary>
/// Analyses source for Dapper syntax and generates suitable interceptors where possible.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DapperInterceptorGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Whether to emit interceptors even if the "interceptors" feature is not detected
    /// </summary>
    public bool OverrideFeatureEnabled { get; set; }

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

    private bool InterceptorsEnabled(SyntaxTree syntaxTree)
            // only even test this for things that look interesting
            => OverrideFeatureEnabled || syntaxTree.Options.Features.ContainsKey("interceptors");

    // very fast and light-weight; we'll worry about the rest later from the semantic tree
    internal static bool IsCandidate(string methodName) => methodName.StartsWith("Execute") || methodName.StartsWith("Query");

    private bool PreFilter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            var name = ma.Name.ToString();
            return IsCandidate(name) && InterceptorsEnabled(node.SyntaxTree);
        }

        return false;
    }

    // support the fact that [DapperAot(bool)] can enable/disable generation at any level
    // including method, type, module and assembly; first attribute found (walking up the tree): wins
    static bool IsEnabled(in GeneratorSyntaxContext ctx, IOperation op, CancellationToken cancellationToken)
    {
        var method = GetContainingMethodSyntax(op);
        if (method is not null)
        {
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
            while (symbol is not null)
            {
                if (HasDapperAotAttribute(symbol, out bool enabled))
                {
                    return enabled;
                }
                symbol = symbol.ContainingSymbol;
            }
        }

        // disabled globally by default
        return false;

        static SyntaxNode? GetContainingMethodSyntax(IOperation op)
        {
            var syntax = op.Syntax;
            while (syntax is not null)
            {
                if (syntax.IsKind(SyntaxKind.MethodDeclaration))
                {
                    return syntax;
                }
                syntax = syntax.Parent;
            }
            return null;
        }
        static bool HasDapperAotAttribute(ISymbol? symbol, out bool enabled)
        {
            if (symbol is not null)
            {
                foreach (var attrib in symbol.GetAttributes())
                {
                    if (attrib.AttributeClass is
                        {
                            Name: "DapperAotAttribute",
                            ContainingNamespace:
                            {
                                Name: "Dapper",
                                ContainingNamespace.IsGlobalNamespace: true
                            }
                        }
                    && attrib.ConstructorArguments.Length == 1
                    && attrib.ConstructorArguments[0].Value is bool b)
                    {
                        enabled = b;
                        return true;
                    }
                }
            }

            enabled = default;
            return false;
        }
    }

    private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.Node is not InvocationExpressionSyntax ie
            || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op)
        {
            return null;
        }
        var methodKind = IsSupportedDapperMethod(op, out var flags);
        switch (methodKind)
        {
            case DapperMethodKind.DapperUnsupported:
                flags |= OperationFlags.DoNotGenerate;
                break;
            case DapperMethodKind.DapperSupported:
                break;
            default: // includes NotDapper
                return null;
        }

        Location? loc = null;

        if (!IsEnabled(ctx, op, cancellationToken))
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"Generation disabled by attribute");
            return null;
        }

        if (op.Syntax.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
        {
            loc = ma.ChildNodes().Skip(1).FirstOrDefault()?.GetLocation();
        }
        loc ??= op.Syntax.GetLocation();

        if (methodKind == DapperMethodKind.DapperUnsupported)
        {
            return new SourceState(Diagnostic.Create(Diagnostics.UnsupportedMethod, loc, GetSignature(op.TargetMethod)));
        }

        object? diagnostics = null;
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
        }

        if (HasAny(flags, OperationFlags.Query))
        {
            if (resultType is null || resultType.SpecialType == SpecialType.System_Object || resultType.TypeKind == TypeKind.Dynamic)
            {
                return new SourceState(Diagnostic.Create(Diagnostics.UntypedResults, loc));
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
                    if (TryGetConstantValue(arg, out bool b))
                    {
                        buffered = b;
                    }
                    break;
                case "param":
                    if (arg.Value is not IDefaultValueOperation)
                    {
                        var expr = arg.Value;
                        if (expr is IConversionOperation conv && expr.Type?.SpecialType == SpecialType.System_Object)
                        {
                            expr = conv.Operand;
                        }
                        paramType = expr?.Type;
                        if (paramType is null || paramType.SpecialType == SpecialType.System_Object)
                        {
                            Log?.Invoke(DiagnosticSeverity.Hidden, $"Cannot use parameter type '{paramType?.Name}'");
                            return null;
                        }
                        flags |= OperationFlags.HasParameters;
                    }
                    break;
                case "cnn":
                case "commandTimeout":
                case "transaction":
                    // nothing to do
                    break;
                case "commandType":
                    if (!TryGetConstantValue(arg, out int? ct))
                    {
                        Log?.Invoke(DiagnosticSeverity.Hidden, $"Non-constant value for '{arg.Parameter?.Name}'");
                        return null;
                    }
                    switch (ct)
                    {
                        case null:
                            break;
                        case 1:
                            flags |= OperationFlags.Text;
                            break;
                        case 4:
                            flags |= OperationFlags.StoredProcedure;
                            break;
                        case 512:
                            flags |= OperationFlags.TableDirect;
                            break;
                        default:
                            Log?.Invoke(DiagnosticSeverity.Hidden, $"Unexpected value for '{arg.Parameter?.Name}'");
                            return null;
                    }
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
        if (HasAny(flags, OperationFlags.Query) && buffered.HasValue)
        {
            flags |= buffered.GetValueOrDefault() ? OperationFlags.Buffered : OperationFlags.Unbuffered;
        }
        Log?.Invoke(DiagnosticSeverity.Hidden, $"OK, {flags}, result-type: {resultType}, param-type: {paramType}");

        return new SourceState(loc, op.TargetMethod, flags, sql, resultType, paramType, "?", diagnostics);

        static void AddDiagnostic(ref object? diagnostics, Diagnostic diagnostic)
        {
            if (diagnostic is null) throw new ArgumentNullException(nameof(diagnostic));
            switch (diagnostics)
            {   // single
                case null:
                    diagnostics = diagnostic;
                    break;
                case Diagnostic d:
                    diagnostics = new List<Diagnostic> { d, diagnostic };
                    break;
                case IList<Diagnostic> list:
                    list.Add(diagnostic);
                    break;
                default:
                    throw new ArgumentException(nameof(diagnostics));
            }
        }

        static bool TryGetConstantValue<T>(IArgumentOperation op, out T? value)
        {
            try
            {
                if (op.ConstantValue.HasValue)
                {
                    value = (T?)op.ConstantValue.Value;
                    return true;
                }
                var val = op.Value;
                if (val is IConversionOperation conv)
                {
                    val = conv.Operand;
                }

                if (val is IFieldReferenceOperation field && field.Field.HasConstantValue)
                {
                    value = (T?)field.Field.ConstantValue;
                    return true;
                }

                if (val is ILiteralOperation or IDefaultValueOperation)
                {
                    var v = val.ConstantValue;
                    value = v.HasValue ? (T?)v.Value : default;
                    return true;
                }
            }
            catch { }
            value = default!;
            return false;
        }
    }

    enum DapperMethodKind
    {
        NotDapper,
        DapperUnsupported,
        DapperSupported,
    }

    static DapperMethodKind IsSupportedDapperMethod(IInvocationOperation operation, out OperationFlags flags)
    {
        flags = OperationFlags.None;
        var method = operation?.TargetMethod;
        if (method is null || !method.IsExtensionMethod)
        {
            return DapperMethodKind.NotDapper;
        }
        var type = method.ContainingType;
        if (type is not { Name: "SqlMapper", ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
        {
            return DapperMethodKind.NotDapper;
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
                flags |= OperationFlags.Query;
                return method.Arity <= 1 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            case "QueryAsync":
            case "QueryUnbufferedAsync":
                flags |= method.Name.Contains("Unbuffered") ? OperationFlags.Unbuffered : OperationFlags.Buffered;
                goto case "Query";
            case "QueryFirst":
            case "QueryFirstAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne;
                goto case "Query";
            case "QueryFirstOrDefault":
            case "QueryFirstOrDefaultAsync":
                flags |= OperationFlags.SingleRow;
                goto case "Query";
            case "QuerySingle":
            case "QuerySingleAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne;
                goto case "Query";
            case "QuerySingleOrDefault":
            case "QuerySingleOrDefaultAsync":
                flags |= OperationFlags.SingleRow | OperationFlags.AtMostOne;
                goto case "Query";
            case "Execute":
            case "ExecuteAsync":
                flags |= OperationFlags.Execute;
                return method.Arity == 0 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            case "ExecuteScalar":
            case "ExecuteScalarAsync":
                flags |= OperationFlags.Execute | OperationFlags.Scalar;
                return method.Arity <= 1 ? DapperMethodKind.DapperSupported : DapperMethodKind.DapperUnsupported;
            default:
                return DapperMethodKind.DapperUnsupported;
        }
    }

    static bool HasAny(OperationFlags value, OperationFlags testFor) => (value & testFor) != 0;
    static bool HasAll(OperationFlags value, OperationFlags testFor) => (value & testFor) == testFor;

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
        Unbuffered = 1 << 6,
        SingleRow = 1 << 7,
        Text = 1 << 8,
        StoredProcedure = 1 << 9,
        TableDirect = 1 << 10,
        AtLeastOne = 1 << 11,
        AtMostOne = 1 << 12,
        Scalar = 1 << 13,
        DoNotGenerate = 1 << 14,
    }

    private static string? GetCommandFactory(Compilation compilation, out bool canConstruct)
    {
        foreach (var attribute in compilation.SourceModule.GetAttributes())
        {
            if(attribute.AttributeClass is {  Name: "CommandFactoryAttribute", Arity: 1, ContainingNamespace: {
                Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
            {
                var type = attribute.AttributeClass.TypeArguments[0];
                canConstruct = false;
                // need non-abstract and public parameterless constructor
                if (!type.IsAbstract && type is INamedTypeSymbol named && named.Arity == 1)
                {
                    foreach (var ctor in named.InstanceConstructors)
                    {
                        if (ctor.Parameters.IsEmpty)
                        {
                            canConstruct = ctor.DeclaredAccessibility == Accessibility.Public;
                            break;
                        }
                    }
                }
                var name = CodeWriter.GetTypeName(type);
                var trimGeneric = name.LastIndexOf('<');
                if (trimGeneric >= 0)
                {
                    name = name.Substring(0, trimGeneric);
                }
                return name;
            }
        }
        canConstruct = true; // we mean the default Dapper one, which can be constructed
        return null;
    }

    private static string GetSignature(IMethodSymbol method, bool deconstruct = true)
    {
        if (deconstruct && method.IsGenericMethod) method = method.ConstructedFrom ?? method;
        return method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
    }
    private void Generate(SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        if (state.Nodes.IsDefaultOrEmpty)
        {
            // nothing to generate
            return;
        }

        int doNotGenerateCount = 0;
        foreach (var node in state.Nodes)
        {
            var count = node.DiagnosticCount;
            for (int i = 0; i < count; i++)
            {
                ctx.ReportDiagnostic(node.GetDiagnostic(i));
            }
            if (HasAny(node.Flags, OperationFlags.DoNotGenerate)) doNotGenerateCount++;
        }

        if (doNotGenerateCount == state.Nodes.Length)
        {
            // nothing but nope
            return;
        }

        var dbCommandTypes = IdentifyDbCommandTypes(state.Compilation, out var needsCommandPrep);

        bool allowUnsafe = state.Compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;
        var sb = new CodeWriter().Append("#nullable enable").NewLine()
            .Append("file static class DapperGeneratedInterceptors").Indent().NewLine();
        int methodIndex = 0;

        var resultTypes = new Dictionary<ITypeSymbol, int>(SymbolEqualityComparer.Default);
        var parameterTypes = new Dictionary<ITypeSymbol, int>(SymbolEqualityComparer.Default);

        foreach (var grp in state.Nodes.Where(x => !HasAny(x.Flags, OperationFlags.DoNotGenerate)).GroupBy(x => x.Group(), CommonComparer.Instance))
        {
            // first, try to resolve the helper method that we're going to use for this
            var (flags, method, parameterType, parameterMap) = grp.Key;
            int arity = HasAny(flags, OperationFlags.TypedResult) ? 2 : 1, argCount = 8;
            bool useUnsafe = false;
            var helperName = method.Name;
            if (helperName == "Query")
            {
                if (HasAny(flags, OperationFlags.Buffered | OperationFlags.Unbuffered))
                {
                    //dedicated mode
                    helperName = HasAny(flags, OperationFlags.Buffered) ? "QueryBuffered" : "QueryUnbuffered";
                }
                else
                {
                    // fallback mode, needs an extra arg to pass in "buffered"
                    argCount++;
                }
            }

            int usageCount = 0;
            foreach (var op in grp.OrderBy(row => row.Location, CommonComparer.Instance))
            {
                var loc = op.Location.GetLineSpan();
                var start = loc.StartLinePosition;
                sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                    .AppendVerbatimLiteral(loc.Path).Append(", ").Append(start.Line + 1).Append(", ").Append(start.Character + 1).Append(")]").NewLine();
                usageCount++;
            }

            if (usageCount == 0)
            {
                continue; // empty group?
            }

            // declare the method
            bool makeMethodNullable = false; // fixup return type annotation
            if (method.ReturnType.IsReferenceType) // (but never change from value-type T to Nullable<T>)
            {
                if (HasAny(flags, OperationFlags.Scalar) && method.Arity == 0)
                {   // ExecuteScalar non-generic returns object when it should return object? - fix that
                    makeMethodNullable = true;
                }
                else if ((flags & (OperationFlags.SingleRow | OperationFlags.AtLeastOne)) == OperationFlags.SingleRow)
                {
                    // FirstOrDefault and SingleOrDefault could return null
                    makeMethodNullable = true;
                }
            }

            sb.Append("internal static ").Append(useUnsafe ? "unsafe " : "").Append(method.ReturnType).Append(makeMethodNullable ? "? " : " ")
                .Append(method.Name).Append(methodIndex++).Append("(");
            var parameters = method.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0) sb.Append(", ");
                else if (method.IsExtensionMethod) sb.Append("this ");
                sb.Append(parameters[i].Type).Append(" ").Append(parameters[i].Name);
            }
            sb.Append(")").Indent().NewLine();
            sb.Append("// ").Append(flags.ToString()).NewLine();
            if (HasAny(flags, OperationFlags.HasParameters))
            {
                sb.Append("// takes parameter: ").Append(parameterType).NewLine();
            }
            ITypeSymbol? resultType = null;
            if (HasAny(flags, OperationFlags.TypedResult))
            {
                resultType = grp.First().ResultType!;
                sb.Append("// returns data: ").Append(resultType).NewLine();
            }

            // assertions
            var commandTypeMode = flags & (OperationFlags.Text | OperationFlags.StoredProcedure | OperationFlags.TableDirect);
            var methodParameters = grp.Key.Method.Parameters;
            if (HasParam(methodParameters, "commandType"))
            {
                switch (commandTypeMode)
                {
                    case 0:
                        sb.Append("global::System.Diagnostics.Debug.Assert(commandType is null);").NewLine();
                        break;
                    default:
                        sb.Append("global::System.Diagnostics.Debug.Assert(commandType == global::System.Data.CommandType.")
                            .Append(commandTypeMode.ToString()).Append(");").NewLine();
                        break;
                }
            }

            if (HasAny(flags, OperationFlags.Buffered | OperationFlags.Unbuffered) && HasParam(methodParameters, "buffered"))
            {
                sb.Append("global::System.Diagnostics.Debug.Assert(buffered is ").Append((flags & OperationFlags.Buffered) != 0).Append(");").NewLine();
            }

            sb.Append("global::System.Diagnostics.Debug.Assert(param is ").Append(HasAny(flags, OperationFlags.HasParameters) ? "not " : "").Append("null);").NewLine().NewLine();

            sb.Append("return ");
            if (HasAll(flags, OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
            {
                sb.Append("global::Dapper.DapperAotExtensions.AsEnumerableAsync(").Indent(false).NewLine();
            }
            // (DbConnection connection, DbTransaction? transaction, string sql, TArgs args, CommandType commandType, int timeout, CommandFactory<TArgs>? commandFactory)
            sb.Append("global::Dapper.DapperAotExtensions.Command<");
            if (parameterType is null || parameterType.IsAnonymousType)
            {
                sb.Append("object?");
            }
            else
            {
                sb.Append(parameterType);
            }
            sb.Append(">(cnn, ").Append(Forward(methodParameters, "transaction")).Append(", sql, ");
            if (parameterType is null || parameterType.IsAnonymousType)
            {
                sb.Append("param");
            }
            else
            {
                sb.Append("(").Append(parameterType).Append(")param");
            }
            sb.Append(", ");
            if (commandTypeMode == 0)
            {   // not hard-coded
                sb.Append(Forward(methodParameters, "commandType")).Append(HasParam(methodParameters, "commandType") ? ".GetValueOrDefault()" : "");
            }
            else
            {
                sb.Append("global::System.Data.CommandType.").Append(commandTypeMode.ToString());
            }
            sb.Append(", ").Append(Forward(methodParameters, "commandTimeout")).Append(HasParam(methodParameters, "commandTimeout") ? " ?? -1" : "").Append(", ");
            if (HasAny(flags, OperationFlags.HasParameters))
            {
                if (!parameterTypes.TryGetValue(parameterType!, out var parameterTypeIndex))
                {
                    parameterTypes.Add(parameterType!, parameterTypeIndex = resultTypes.Count);
                }
                sb.Append("CommandFactory").Append(parameterTypeIndex).Append(".Instance");
            }
            else
            {
                sb.Append("DefaultCommandFactory");
            }
            sb.Append(").");

            bool isAsync = HasAny(flags, OperationFlags.Async);
            if (HasAny(flags, OperationFlags.Query))
            {
                sb.Append("Query").Append((flags & (OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne)) switch
                {
                    OperationFlags.SingleRow => "FirstOrDefault",
                    OperationFlags.SingleRow | OperationFlags.AtLeastOne => "First",
                    OperationFlags.SingleRow | OperationFlags.AtMostOne => "SingleOrDefault",
                    OperationFlags.SingleRow | OperationFlags.AtLeastOne | OperationFlags.AtMostOne => "Single",
                    _ => "",
                }).Append((flags & (OperationFlags.Buffered | OperationFlags.Unbuffered)) switch
                {
                    OperationFlags.Buffered => "Buffered",
                    OperationFlags.Unbuffered => "Unbuffered",
                    _ => ""
                }).Append(isAsync ? "Async" : "").Append("<").Append(resultType).Append(">").Append("(");
                if (!HasAny(flags, OperationFlags.SingleRow))
                {
                    switch (flags & (OperationFlags.Buffered | OperationFlags.Unbuffered))
                    {
                        case OperationFlags.Buffered:
                        case OperationFlags.Unbuffered:
                            break;
                        default: // using "could be either" buffer mode
                            sb.Append(Forward(methodParameters, "buffered")).Append(", ");
                            break;
                    }
                }
                if (!resultTypes.TryGetValue(resultType!, out var resultTypeIndex))
                {
                    resultTypes.Add(resultType!, resultTypeIndex = resultTypes.Count);
                }
                sb.Append("RowFactory").Append(resultTypeIndex).Append(".Instance").Append(isAsync ? ", " : "");
            }
            else if (HasAny(flags, OperationFlags.Execute))
            {
                sb.Append("Execute");
                if (HasAny(flags, OperationFlags.Scalar))
                {
                    sb.Append("Scalar");
                }
                sb.Append(isAsync ? "Async" : "");
                if (method.Arity == 1)
                {
                    sb.Append("<").Append(resultType).Append(">");
                }
                sb.Append("(");
            }
            else
            {
                sb.NewLine().Append("#error not supported: ").Append(method.Name).NewLine();
            }
            if (isAsync)
            {
                sb.Append(Forward(methodParameters, "cancellationToken"));
            }
            if (HasAll(flags, OperationFlags.Async | OperationFlags.Query | OperationFlags.Buffered))
            {
                sb.Append(")").Outdent(false);
            }
            sb.Append(");");
            sb.NewLine().Outdent().NewLine().NewLine();

            static bool HasParam(ImmutableArray<IParameterSymbol> methodParameters, string name)
            {
                foreach (var p in methodParameters)
                {
                    if (p.Name == name)
                    {
                        return true;
                    }
                }
                return false;
            }
            static string Forward(ImmutableArray<IParameterSymbol> methodParameters, string name)
                => HasParam(methodParameters, name) ? name : "default";
        }

        const string DapperBaseCommandFactory = "global::Dapper.CommandFactory";
        var baseCommandFactory = GetCommandFactory(state.Compilation, out var canConstruct) ?? DapperBaseCommandFactory;
        if (needsCommandPrep || !canConstruct)
        {
            // at least one command-type needs special handling; do that
            sb.Append("private class CommonCommandFactory<T> : ").Append(baseCommandFactory).Append("<T>").Indent().NewLine();
            if (needsCommandPrep)
            {
                sb.Append("public override global::System.Data.Common.DbCommand GetCommand(global::System.Data.Common.DbConnection connection, string sql, global::System.Data.CommandType commandType, T args)").Indent().NewLine()
                .Append("var cmd = base.GetCommand(connection, sql, commandType, args);");
                int cmdTypeIndex = 0;
                foreach (var type in dbCommandTypes)
                {
                    var flags = GetSpecialCommandFlags(type);
                    if (flags != SpecialCommandFlags.None)
                    {
                        sb.NewLine().Append("// apply special per-provider command initialization logic for ").Append(type.Name).NewLine()
                            .Append(cmdTypeIndex == 0 ? "" : "else ").Append("if (cmd is ").Append(type).Append(" cmd").Append(cmdTypeIndex).Append(")").Indent().NewLine();
                        if ((flags & SpecialCommandFlags.BindByName) != 0)
                        {
                            sb.Append("cmd").Append(cmdTypeIndex).Append(".BindByName = true;").NewLine();
                        }
                        if ((flags & SpecialCommandFlags.InitialLONGFetchSize) != 0)
                        {
                            sb.Append("cmd").Append(cmdTypeIndex).Append(".InitialLONGFetchSize = -1;").NewLine();
                        }
                        sb.Outdent().NewLine();
                        cmdTypeIndex++;
                    }
                }
                sb.Append("return cmd;").Outdent().NewLine();
            }
            sb.Outdent().NewLine();
            baseCommandFactory = "CommonCommandFactory";
        }

        // add in DefaultCommandFactory as a short-hand to a non-null basic factory
        sb.NewLine();
        if (baseCommandFactory == DapperBaseCommandFactory)
        {
            sb.Append("private static ").Append(baseCommandFactory).Append("<object?> DefaultCommandFactory => ")
                .Append(baseCommandFactory).Append(".Simple;").NewLine();
        }
        else
        {
            sb.Append("private static readonly ").Append(baseCommandFactory).Append("<object?> DefaultCommandFactory = new();").NewLine();
        }
        sb.NewLine();

        foreach (var pair in resultTypes.OrderBy(x => x.Value)) // retain discovery order
        {
            WriteRowFactory(sb, pair.Key, pair.Value);
        }

        foreach (var pair in parameterTypes.OrderBy(x => x.Value)) // retain discovery order
        {
            WriteCommandFactory(baseCommandFactory, sb, pair.Key, pair.Value);
        }

        sb.Outdent(); // ends our generated file-scoped class

        // we need an accessible [InterceptsLocation] - if not; add our own in the generated code
        var attrib = state.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InterceptsLocationAttribute");
        if (attrib is null || attrib.DeclaredAccessibility != Accessibility.Public)
        {
            sb.NewLine().Append(Resources.ReadString("Dapper.InterceptsLocationAttribute.cs"));
        }
        ctx.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void WriteCommandFactory(string baseFactory, CodeWriter sb, ITypeSymbol type, int index)
    {
        var declaredType = type.IsAnonymousType ? "object?" : CodeWriter.GetTypeName(type);
        sb.Append("private sealed class CommandFactory").Append(index).Append(" : ")
            .Append(baseFactory).Append("<").Append(declaredType).Append(">");
        if (type.IsAnonymousType)
        {
            sb.Append(" // ").Append(type); // give the reader a clue
        }
        sb.Indent().NewLine()
            .Append("internal static readonly CommandFactory").Append(index).Append(" Instance = new();").NewLine()
            .Append("private CommandFactory").Append(index).Append("() {}").NewLine()
            .Append("public override void AddParameters(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine()
            .Append("// var sql = cmd.CommandText;").NewLine()
            .Append("// var commandType = cmd.CommandType;").NewLine();
        var argSource = "args";
        if (type.IsAnonymousType)
        {
            sb.Append("var typed = Cast(args, ");
            AppendShapeLambda(sb, type);
            sb.Append("); // expected shape").NewLine();
            argSource = "typed";
        }
        WriteArgs(type, sb, argSource, true);
        sb.Outdent().NewLine();

        sb.Append("public override void UpdateParameters(global::System.Data.Common.DbCommand cmd, ").Append(declaredType).Append(" args)").Indent().NewLine().Append("var sql = cmd.CommandText;").NewLine();
        if (type.IsAnonymousType)
        {
            sb.Append("var typed = Cast(args, ");
            AppendShapeLambda(sb, type);
            sb.Append("); // expected shape").NewLine();
            argSource = "typed";
        }
        WriteArgs(type, sb, argSource, false);
        sb.Outdent().NewLine();


        sb.Outdent().NewLine().NewLine();
    }

    private static void WriteRowFactory(CodeWriter sb, ITypeSymbol type, int index)
    {
        var members = type.GetMembers();
        var memberCount = 0;
        foreach (var member in members)
        {
            if (CodeWriter.IsSettableInstanceMember(member, out _))
            {
                memberCount++;
            }
        }

        sb.Append("private sealed class RowFactory").Append(index).Append(" : global::Dapper.RowFactory").Append("<").Append(type).Append(">")
            .Indent().NewLine()
            .Append("internal static readonly RowFactory").Append(index).Append(" Instance = new();").NewLine()
            .Append("private RowFactory").Append(index).Append("() {}").NewLine();

        if (memberCount != 0)
        {
            sb.Append("public override void Tokenize(global::System.Data.Common.DbDataReader reader, global::System.Span<int> tokens, int columnOffset)").Indent().NewLine();
            sb.Append("for (int i = 0; i < tokens.Length; i++)").Indent().NewLine()
                .Append("int token = -1;").NewLine()
                .Append("var name = reader.GetName(columnOffset);").NewLine()
                .Append("var type = reader.GetFieldType(columnOffset);").NewLine()
                .Append("switch (NormalizedHash(name))").Indent().NewLine();

            int token = 0;
            foreach (var member in members)
            {
                if (CodeWriter.IsSettableInstanceMember(member, out var memberType))
                {
                    var name = member.Name; // TODO: [Column] ?
                    sb.Append("case ").Append(StringHashing.NormalizedHash(name))
                        .Append(" when NormalizedEquals(name, ")
                        .AppendVerbatimLiteral(StringHashing.Normalize(name)).Append("):").Indent(false).NewLine()
                        .Append("token = type == typeof(").Append(MakeNonNullable(memberType)).Append(") ? ").Append(token)
                        .Append(" : ").Append(token + memberCount).Append(";")
                        .Append(token == 0 ? " // two tokens for right-typed and type-flexible" : "").NewLine()
                        .Append("break;").Outdent(false).NewLine();
                    token++;
                }
            }
            sb.Outdent().NewLine()
                .Append("tokens[i] = token;").NewLine()
                .Append("columnOffset++;").NewLine();
            sb.Outdent().NewLine().Outdent().NewLine();
        }
        sb.Append("public override ").Append(type).Append(" Read(global::System.Data.Common.DbDataReader reader, global::System.ReadOnlySpan<int> tokens, int columnOffset)").Indent().NewLine();

        sb.Append(type.NullableAnnotation == NullableAnnotation.Annotated
            ? type.WithNullableAnnotation(NullableAnnotation.None) : type).Append(" result = new();").NewLine();

        if (memberCount != 0)
        {
            sb.Append("foreach (var token in tokens)").Indent().NewLine()
            .Append("switch (token)").Indent().NewLine();

            int token = 0;
            foreach (var member in members)
            {
                if (CodeWriter.IsSettableInstanceMember(member, out var memberType))
                {
                    IdentifyDbType(memberType, out var readerMethod);
                    var nullCheck = CouldBeNullable(memberType) ? $"reader.IsDBNull(columnOffset) ? ({CodeWriter.GetNullableTypeName(memberType)})null : " : "";
                    sb.Append("case ").Append(token).Append(":").NewLine().Indent(false)
                        .Append("result.").Append(member.Name).Append(" = ").Append(nullCheck);

                    if (readerMethod is null)
                    {
                        sb.Append("reader.GetFieldValue<").Append(memberType).Append(">(columnOffset);");
                    }
                    else
                    {
                        sb.Append("reader.").Append(readerMethod).Append("(columnOffset);");
                    }
                    sb.NewLine().Append("break;").NewLine().Outdent(false)
                        .Append("case ").Append(token + memberCount).Append(":").NewLine().Indent(false)
                        .Append("result.").Append(member.Name).Append(" = ").Append(nullCheck)
                        .Append("GetValue<")
                        .Append(MakeNonNullable(memberType)).Append(">(reader, columnOffset);").NewLine()
                        .Append("break;").NewLine().Outdent(false);
                    token++;
                }
            }

            sb.Outdent().NewLine().Append("columnOffset++;").NewLine();
        }
        sb.Outdent().NewLine().Append("return result;").NewLine().Outdent().NewLine();


        sb.Outdent().NewLine().NewLine();




        static bool CouldBeNullable(ITypeSymbol symbol) => symbol.IsValueType
            ? symbol.NullableAnnotation == NullableAnnotation.Annotated
            : symbol.NullableAnnotation != NullableAnnotation.NotAnnotated;
    }

    private static void WriteArgs(ITypeSymbol? parameterType, CodeWriter sb, string source, bool add)
    {
        if (parameterType is null)
        {
            return;
        }
        var members = parameterType.GetMembers();
        bool first = true;
        foreach (var member in members)
        {
            if (CodeWriter.IsGettableInstanceMember(member, out var type))
            {
                var name = member.Name; // TODO use [Column] here?
                if (first)
                {
                    if (add)
                    {
                        sb.Append("global::System.Data.Common.DbParameter p;").NewLine();
                    }
                    else
                    {
                        sb.Append("var ps = cmd.Parameters;").NewLine();
                    }
                    
                    first = false;
                }
                sb.Append("// if (Include(sql, commandType, ").AppendVerbatimLiteral(name).Append("))").Indent().NewLine();
                if (add)
                {
                    sb.Append("p = cmd.CreateParameter();").NewLine()
                        .Append("p.ParameterName = ").AppendVerbatimLiteral(name).Append(";").NewLine();
                    var dbType = IdentifyDbType(type, out _);
                    if (dbType is not null)
                    {
                        sb.Append("p.DbType = global::System.Data.DbType.").Append(dbType.ToString()).Append(";").NewLine();
                    }
                    sb.Append("p.Value = AsValue(").Append(source).Append(".").Append(member.Name).Append(");").NewLine()
                        .Append("cmd.Parameters.Add(p);");
                }
                else
                {
                    sb.Append("ps[").AppendVerbatimLiteral(name).Append("].Value = AsValue(").Append(source).Append(".").Append(member.Name).Append(");").NewLine();
                }
                sb.Outdent().NewLine();
            }
        }
    }

    private static ITypeSymbol MakeNonNullable(ITypeSymbol type)
    {
        // think: type = Nullable.GetUnderlyingType(type) ?? type
        if (type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated
            && type is INamedTypeSymbol named && named.Arity == 1)
        {
            type = named.TypeArguments[0];
        }
        return type.NullableAnnotation == NullableAnnotation.None
            ? type : type.WithNullableAnnotation(NullableAnnotation.None);
    }
    private static DbType? IdentifyDbType(ITypeSymbol type, out string? readerMethod)
    {
        type = MakeNonNullable(type);
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
                readerMethod = nameof(DbDataReader.GetBoolean);
                return DbType.Boolean;
            case SpecialType.System_String:
                readerMethod = nameof(DbDataReader.GetString);
                return DbType.String;
            case SpecialType.System_Single:
                readerMethod = nameof(DbDataReader.GetFloat);
                return DbType.Single;
            case SpecialType.System_Double:
                readerMethod = nameof(DbDataReader.GetDouble);
                return DbType.Double;
            case SpecialType.System_Decimal:
                readerMethod = nameof(DbDataReader.GetDecimal);
                return DbType.Decimal;
            case SpecialType.System_DateTime:
                readerMethod = nameof(DbDataReader.GetDateTime);
                return DbType.DateTime;
            case SpecialType.System_Int16:
                readerMethod = nameof(DbDataReader.GetInt16);
                return DbType.Int16;
            case SpecialType.System_Int32:
                readerMethod = nameof(DbDataReader.GetInt32);
                return DbType.Int32;
            case SpecialType.System_Int64:
                readerMethod = nameof(DbDataReader.GetInt64);
                return DbType.Int64;
            case SpecialType.System_UInt16:
                readerMethod = null;
                return DbType.UInt16;
            case SpecialType.System_UInt32:
                readerMethod = null;
                return DbType.UInt32;
            case SpecialType.System_UInt64:
                readerMethod = null;
                return DbType.UInt64;
            case SpecialType.System_Byte:
                readerMethod = nameof(DbDataReader.GetByte);
                return DbType.Byte;
            case SpecialType.System_SByte:
                readerMethod = null;
                return DbType.SByte;
        }
        //if (type is IArrayTypeSymbol array && array.IsSZArray) // SZArray === "vector" (1-dim, 0-based)
        //{
        // byte
        //}

        if (type.Name == nameof(Guid) && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            readerMethod = nameof(DbDataReader.GetGuid);
            return DbType.Guid;
        }
        if (type.Name == nameof(DateTimeOffset) && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true })
        {
            readerMethod = null;
            return DbType.DateTimeOffset;
        }
        readerMethod = null;
        return null;
    }

    private static void AppendShapeLambda(CodeWriter sb, ITypeSymbol parameterType)
    {
        var members = parameterType.GetMembers();
        int count = CodeWriter.CountGettableInstanceMembers(members);
        switch (count)
        {
            case 0:
                sb.Append("static () => (object?)null");
                break;
            default:
                bool first = true;
                sb.Append("static () => new {");
                foreach (var member in members)
                {
                    if (CodeWriter.IsGettableInstanceMember(member, out var type))
                    {
                        sb.Append(first ? " " : ", ").Append(member.Name).Append(" = default(").Append(type).Append(")");
                        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None)
                        {
                            sb.Append("!");
                        }
                        first = false;
                    }
                }
                sb.Append(" }");
                break;
        }
    }

    private static SpecialCommandFlags GetSpecialCommandFlags(ITypeSymbol type)
    {
        // check whether these command-types need special handling
        var flags = SpecialCommandFlags.None;
        foreach (var member in type.GetMembers())
        {
            switch (member.Name)
            {
                // just do a quick check for now, will be close enough
                case "BindByName" when IsSettableInstanceProperty(member, SpecialType.System_Boolean):
                    flags |= SpecialCommandFlags.BindByName;
                    break;
                case "InitialLONGFetchSize" when IsSettableInstanceProperty(member, SpecialType.System_Int32):
                    flags |= SpecialCommandFlags.InitialLONGFetchSize;
                    break;
            }
        }
        return flags;

        static bool IsSettableInstanceProperty(ISymbol? symbol, SpecialType type) =>
            symbol is IPropertySymbol prop && prop.DeclaredAccessibility == Accessibility.Public
            && prop.SetMethod is { DeclaredAccessibility: Accessibility.Public }
            && prop.Type.SpecialType == type
            && !prop.IsIndexer && !prop.IsStatic;
    }

    [Flags]
    private enum SpecialCommandFlags
    {
        None = 0,
        BindByName = 1 << 0,
        InitialLONGFetchSize = 1 << 1,
    }

    private ImmutableArray<ITypeSymbol> IdentifyDbCommandTypes(Compilation compilation, out bool needsPrepare)
    {
        needsPrepare = false;
        var dbCommand = compilation.GetTypeByMetadataName("System.Data.Common.DbCommand");
        if (dbCommand is null)
        {
            // if we can't find DbCommand, we're out of luck
            return ImmutableArray<ITypeSymbol>.Empty;
        }
        var pending = new Queue<INamespaceOrTypeSymbol>();
        foreach (var assemblyName in compilation.References)
        {
            if (assemblyName is null) continue;
            var ns = compilation.GetAssemblyOrModuleSymbol(assemblyName) switch
            {
                IAssemblySymbol assembly => assembly.GlobalNamespace,
                IModuleSymbol module => module.GlobalNamespace,
                _ => null
            };
            if (ns is not null)
            {
                pending.Enqueue(ns);
            }
        }
        var found = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        while (pending.Count != 0)
        {
            var current = pending.Dequeue();
            foreach (var member in current.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        pending.Enqueue(ns);
                        break;
                    case ITypeSymbol type:
                        // only interested in public non-static classes
                        if (!type.IsStatic && type.TypeKind == TypeKind.Class && type.DeclaredAccessibility == Accessibility.Public)
                        {
                            // note we're not checking for nested types; that seems incredibly unlikely for ADO.NET types
                            if (IsDerived(type, dbCommand))
                            {
                                found.Add(type);
                            }
                        }
                        break;
                }
            }
        }

        foreach (var type in found)
        {
            if (GetSpecialCommandFlags(type) != SpecialCommandFlags.None)
            {
                needsPrepare = true;
                break; // only need at least one
            }
        }
        return found.ToImmutableArray();

        static bool IsDerived(ITypeSymbol? type, ITypeSymbol baseType)
        {
            while (type is not null && type.SpecialType != SpecialType.System_Object)
            {
                type = type.BaseType;
                if (SymbolEqualityComparer.Default.Equals(type, baseType))
                {
                    return true;
                }
            }
            return false;
        }
    }

    sealed class SourceState
    {
        private object? diagnostics;

        public Location Location { get; }
        public OperationFlags Flags { get; }
        public string? Sql { get; }
        public string ParameterMap { get; }
        public IMethodSymbol Method { get; }
        public ITypeSymbol? ResultType { get; }
        public ITypeSymbol? ParameterType { get; }

        public SourceState(Location location, IMethodSymbol method, OperationFlags flags, string? sql,
            ITypeSymbol? resultType, ITypeSymbol? parameterType, string parameterMap, object? diagnostics = null)
        {
            Location = location;
            Flags = flags;
            Sql = sql;
            ResultType = resultType;
            ParameterType = parameterType;
            Method = method;
            ParameterMap = parameterMap;
            this.diagnostics = diagnostics;
        }
        public SourceState(object diagnostics)
        {
            Location = Location.None;
            Flags = OperationFlags.DoNotGenerate;
            ParameterMap = Sql = "";
            ResultType = ParameterType = null;
            Method = null!;
            this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public int DiagnosticCount => diagnostics switch
        {
            null => 0,
            Diagnostic => 1,
            IReadOnlyList<Diagnostic> list => list.Count,
            _ => -1
        };

        public Diagnostic GetDiagnostic(int index) => diagnostics switch
        {
            Diagnostic d when index is 0 => d,
            IReadOnlyList<Diagnostic> list => list[index],
            _ => throw new IndexOutOfRangeException(nameof(index)),
        };

        public (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap) Group() => new(Flags, Method, ParameterType, ParameterMap);
    }
    private sealed class CommonComparer : IEqualityComparer<(OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap)>, IComparer<Location>
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

        public bool Equals((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap) x, (OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap) y) => x.Flags == y.Flags
                && x.ParameterMap == y.ParameterMap
                && SymbolEqualityComparer.Default.Equals(x.Method, y.Method)
                && SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType);

        public int GetHashCode((OperationFlags Flags, IMethodSymbol Method, ITypeSymbol? ParameterType, string ParameterMap) obj)
        {
            var hash = (int)obj.Flags;
            hash *= -47;
            hash += obj.ParameterMap.GetHashCode();
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
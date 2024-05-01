using Dapper.CodeAnalysis.Abstractions;
using Dapper.CodeAnalysis.Writers;
using Dapper.Internal;
using Dapper.Internal.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using static Dapper.CodeAnalysis.DapperInterceptorGenerator;

namespace Dapper.CodeAnalysis;

[Generator(LanguageNames.CSharp), DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class TypeAccessorInterceptorGenerator : InterceptorGeneratorBase
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticsBase.All<Diagnostics>();

    public event Action<DiagnosticSeverity, string>? Log;

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
            return memberAccess.Expression.ToString() == "TypeAccessor" && (memberAccess.Name.ToString() is "CreateAccessor" or "CreateDataReader");
        }

        return false;
    }

    private SourceState? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        if (ctx.Node is not InvocationExpressionSyntax ie || ctx.SemanticModel.GetOperation(ie, cancellationToken) is not IInvocationOperation op)
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

        return new SourceState(loc!, parameterType!, op.TargetMethod);

        bool TryParseParameterType(out ITypeSymbol? type)
        {
            if (op.TargetMethod.IsGenericMethod && op.TargetMethod.Arity == 1)
            {
                type = op.TargetMethod.TypeArguments[0];
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

    private void Generate(SourceProductionContext context, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
    {
        if (!IsGenerateInputValid(ref context, state))
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"Generate input for '{nameof(TypeAccessorInterceptorGenerator)}' does not allow generation.");
            return;
        }

        var codeWriter = new CodeWriter();
        var sb = new TypeAccessorInterceptorCodeWriter(codeWriter);

        sb.WriteFileHeader(state.Compilation);
        sb.WriteInterceptorsClass(() =>
        {
            int typeCounter = -1, methodCounter = 0;
            foreach (var group in state.Nodes.GroupBy(x => x, SourceStateByTypeComparer.Instance))
            {
                typeCounter++;

                var typeSymbol = group.Key.ParameterType;

                // not allowing collections
                if (Inspection.IsCollectionType(typeSymbol, out _))
                {
                    ReportDiagnosticInUsages(Diagnostics.TypeAccessorCollectionTypeNotAllowed);
                    continue;
                }

                // not allowing primitives
                if (Inspection.IsPrimitiveType(typeSymbol))
                {
                    ReportDiagnosticInUsages(Diagnostics.TypeAccessorPrimitiveTypeNotAllowed);
                    continue;
                }

                var typeSymbolName = CodeWriter.GetTypeName(typeSymbol);
                var members = ConstructTypeMembers(typeSymbol!);
                if (members.Length == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeAccessorMembersNotParsed, null));
                    continue;
                }

                foreach (var methodGroup in group.GroupBy(x => x.Method, SymbolEqualityComparer.Default))
                {
                    foreach (var usage in methodGroup)
                    {
                        sb.WriteInterceptorsLocationAttribute(usage.Location);
                    }
                    sb.WriteMethodForwarder((IMethodSymbol)methodGroup.Key!, typeCounter, ref methodCounter);
                }

                var accessorSb = new CustomTypeAccessorClassCodeWriter(codeWriter);
                accessorSb.WriteClass(typeCounter, typeSymbolName, () =>
                {
                    accessorSb.WriteMemberCount(members.Length);
                    accessorSb.WriteTryIndex(typeSymbolName, members);
                    accessorSb.WriteGetName(typeSymbolName, members);
                    accessorSb.WriteIndexer(typeSymbolName, members);
                    accessorSb.WriteIsNullable(members);
                    accessorSb.WriteIsNull(typeSymbolName, members);
                    accessorSb.WriteGetType(members);
                    accessorSb.WriteGetValue(typeSymbolName, members);
                    accessorSb.WriteSetValue(typeSymbolName, members);
                });

                void ReportDiagnosticInUsages(DiagnosticDescriptor diagnosticDescriptor)
                {
                    foreach (var usage in group)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, usage.Location));
                    }
                }
            }
        });

        var preGenerator = new PreGeneratedCodeWriter(codeWriter, state.Compilation);
        preGenerator.Write(IncludedGeneration.InterceptsLocationAttribute);

        context.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", sb.GetSourceText());
    }

    private static bool IsGenerateInputValid(ref SourceProductionContext ctx, (Compilation Compilation, ImmutableArray<SourceState> Nodes) state)
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
        public IMethodSymbol Method { get; }

        public SourceState(
            Location location,
            ITypeSymbol parameterType,
            IMethodSymbol method)
        {
            Location = location;
            ParameterType = parameterType;
            Method = method;
        }

        public (ITypeSymbol ParameterType, Location? UniqueLocation) Group()
            => new(ParameterType, Location);
    }

    [DebuggerDisplay("code: '{_sb.ToString()}'")]
    readonly struct TypeAccessorInterceptorCodeWriter
    {
        readonly CodeWriter _sb = new();
        public TypeAccessorInterceptorCodeWriter(CodeWriter codeWriter)
        {
            _sb = codeWriter;
        }

        public void WriteFileHeader(Compilation compilation)
        {
            bool allowUnsafe = compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe;
            if (allowUnsafe)
            {
                _sb.Append("#nullable enable").NewLine();
            }
        }

        public void WriteInterceptorsClass(Action innerWriter)
        {
            _sb.Append("#nullable enable").NewLine()
                .Append("namespace ").Append(FeatureKeys.CodegenNamespace)
                .Append(" // interceptors must be in a known namespace").Indent().NewLine()
                .Append("file static class DapperTypeAccessorGeneratedInterceptors").Indent().NewLine();
            innerWriter();
            _sb.Outdent().Outdent();
        }

        public void WriteInterceptorsLocationAttribute(Location location)
        {
            var loc = location.GetLineSpan();
            var start = loc.StartLinePosition;
            _sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                .AppendVerbatimLiteral(loc.Path).Append(", ").Append(start.Line + 1).Append(", ").Append(start.Character + 1).Append(")]")
                .NewLine();
        }

        public void WriteMethodForwarder(IMethodSymbol method, int customTypeNum, ref int methodNumber)
        {
            _sb.Append("internal static ").Append(method.ReturnType).Append(" ").Append("Forwarded").Append(methodNumber++).Append("(");
            int i = 0;
            foreach (var arg in method.Parameters)
            {
                _sb.Append(i == 0 ? "" : ", ").Append(arg.Type).Append(" ").Append(arg.Name);
                i++;
            }
            _sb.Append(")").Indent(false).NewLine().Append("=> ");

            _sb.Append(method.ContainingType).Append(".").Append(method.Name).Append("(");
            i = 0;
            foreach (var arg in method.Parameters)
            {
                _sb.Append(i == 0 ? "" : ", ").Append(arg.Name);
                if (arg.Type is INamedTypeSymbol { IsGenericType: true, Arity: 1, Name: "TypeAccessor", ContainingType: null, ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } })
                {
                    _sb.Append(" ?? ").Append(GetCustomTypeAccessorClassName(customTypeNum)).Append(".Instance");
                }
                i++;
            }
            _sb.Append(");").Outdent(false).NewLine().NewLine();

            //_sb.Append("public static global::Dapper.ObjectAccessor<").Append(userTypeName).Append("> ")
            //   .Append("CreateAccessor(").Append(userTypeName).Append(" obj, ")
            //   .Append("global::Dapper.TypeAccessor<").Append(userTypeName).Append(">? accessor = null)")
            //   .Indent().NewLine();

            //_sb.Append("return new global::Dapper.ObjectAccessor<").Append(userTypeName).Append(">")
            //   .Append("(obj, accessor ?? ").Append(GetCustomTypeAccessorClassName(customTypeNum)).Append(".Instance);")
            //   .Outdent().NewLine().NewLine();
        }

        public SourceText GetSourceText() => SourceText.From(_sb.ToString(), Encoding.UTF8);
    }

    [DebuggerDisplay("code: '{_sb.ToString()}'")]
    readonly struct CustomTypeAccessorClassCodeWriter
    {
        readonly CodeWriter _sb;
        public CustomTypeAccessorClassCodeWriter(CodeWriter codeWriter)
        {
            _sb = codeWriter;
        }

        public void WriteClass(int customTypeNum, string userType, Action innerWriter)
        {
            var className = GetCustomTypeAccessorClassName(customTypeNum);

            _sb.Append("private sealed class " + className + " : global::Dapper.TypeAccessor<").Append(userType).Append(">")
               .Indent().NewLine()
               .Append($"internal static readonly {className} Instance = new();")
               .NewLine();
            innerWriter();
            _sb.Outdent().NewLine();
        }

        public void WriteMemberCount(int memberCount)
            => _sb.Append("public override int MemberCount => ").Append(memberCount).Append(";").NewLine();

        public void WriteTryIndex(string userTypeName, MemberData[] members)
        {
            var sb = _sb;
            sb.Append("public override int? TryIndex(string name, bool exact = false)")
               .Indent().NewLine();

            sb.Append("if (exact)").Indent().NewLine();
            WriteDefaultImplementation();
            sb.Outdent().NewLine();

            sb.Append("else").Indent().NewLine();
            WriteHashVersionImplementation();
            sb.Outdent();

            sb.Outdent().NewLine();

            void WriteDefaultImplementation()
            {
                sb.Append("return name switch").Indent().NewLine();
                foreach (var member in members)
                {
                    sb.Append("nameof(").Append($"{userTypeName}.{member.Name}").Append(") => ").Append(member.Number).Append(",").NewLine();
                }

                sb.Append("_ => base.TryIndex(name, exact)")
                   .Outdent().Append(";");
            }

            void WriteHashVersionImplementation()
            {
                sb.Append("return NormalizedHash(name) switch").Indent().NewLine();
                foreach (var member in members)
                {
                    sb.Append(StringHashing.NormalizedHash(member.Name)).Append(" when NormalizedEquals(name, ").AppendVerbatimLiteral(StringHashing.Normalize(member.Name))
                      .Append(") => ").Append(member.Number).Append(",").NewLine();
                }

                sb.Append("_ => base.TryIndex(name, exact)")
                   .Outdent().Append(";");
            }
        }

        public void WriteGetName(string userTypeName, MemberData[] members)
        {
            _sb.Append("public override string GetName(int index) => index switch")
               .Indent().NewLine();

            foreach (var member in members)
            {
                _sb.Append(member.Number).Append(" => nameof(").Append($"{userTypeName}.{member.Name}").Append("),").NewLine();
            }

            _sb.Append("_ => base.GetName(index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteIndexer(string userTypeName, MemberData[] members)
        {
            _sb.Append("public override object? this[").Append(userTypeName).Append(" obj, int index]")
               .Indent().NewLine();

            _sb.Append("get => index switch").Indent().NewLine();
            foreach (var member in members)
            {
                _sb.Append(member.Number).Append(" => obj.").Append(member.Name).Append(",").NewLine();
            }
            _sb.Append("_ => base[obj, index]").Outdent().Append(";").NewLine();


            _sb.Append("set").Indent().NewLine()
               .Append("switch (index)").Indent().NewLine();
            foreach (var member in members)
            {
                _sb.Append("case ").Append(member.Number).Append(": obj.")
                   .Append(member.Name).Append(" = (").Append(member.Type).Append(")value!; break;").NewLine();
            }
            _sb.Append("default: base[obj, index] = value; break;")
               .Outdent().Append(";").Outdent();

            _sb.Outdent().NewLine();
        }

        public void WriteIsNullable(MemberData[] members)
        {
            _sb.Append("public override bool IsNullable(int index) => index switch")
               .Indent().NewLine();

            var strBuilder = new StringBuilder();
            foreach (var item in members.Where(x => x.IsNullable))
            {
                strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => true,").NewLine();
            }

            strBuilder.Clear();
            foreach (var item in members.Where(x => !x.IsNullable))
            {
                strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => false,").NewLine();
            }

            _sb.Append("_ => base.IsNullable(index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteIsNull(string userTypeName, MemberData[] members)
        {
            _sb.Append("public override bool IsNull(").Append(userTypeName).Append(" obj, int index) => index switch")
               .Indent().NewLine();

            var strBuilder = new StringBuilder();
            foreach (var item in members.Where(x => !x.IsNullable))
            {
                strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => false,").NewLine();
            }

            foreach (var member in members.Where(x => x.IsNullable))
            {
                if (IsDBNull(member.TypeSymbol))
                {
                    // if member is of type DBNull, then it is always null => simply return true
                    _sb.Append(member.Number).Append(" => true,").NewLine();
                    continue;
                }

                _sb.Append(member.Number).Append(" => obj.").Append(member.Name).Append(" is null");
                if (member.TypeSymbol.IsSystemObject())
                {
                    _sb.Append(" or global::System.DBNull");
                }
                _sb.Append(",").NewLine();
            }

            _sb.Append("_ => base.IsNull(obj, index)")
               .Outdent().Append(";").NewLine();

            static bool IsDBNull(ITypeSymbol typeSymbol)
            {
                return typeSymbol.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true
                    && typeSymbol.ContainingNamespace.Name == "System"
                    && typeSymbol.Name == "DBNull";
            }
        }

        public void WriteGetType(MemberData[] members)
        {
            _sb.Append("public override global::System.Type GetType(int index) => index switch")
               .Indent().NewLine();

            var tmpSb = new StringBuilder();
            foreach (var typeGroup in members.GroupBy(x => x.Type))
            {
                tmpSb.Clear();
                foreach (var mem in typeGroup)
                {
                    tmpSb.Append(mem.Number).Append(" or ");
                }
                tmpSb.Length -= 4;
                _sb.Append(tmpSb.ToString()).Append(" => typeof(").Append(typeGroup.Key).Append("),").NewLine();
            }

            _sb.Append("_ => base.GetType(index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteGetValue(string userTypeName, MemberData[] members)
        {
            _sb.Append("public override TValue GetValue<TValue>(").Append(userTypeName).Append(" obj, int index) => index switch")
                .Indent().NewLine();

            foreach (var member in members)
            {
                _sb.Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append(")");

                // if memberType is enum, we need to figure out an underlying type and check on it
                var underlyingType = member.TypeSymbol.GetUnderlyingEnumTypeName();
                if (underlyingType is not null)
                {
                    _sb.Append(" || typeof(TValue) == typeof(").Append(underlyingType).Append(")");
                }

                _sb.Append(" => UnsafePun<").Append(member.Type).Append(", TValue>(obj.").Append(member.Name).Append("),").NewLine();
            }

            _sb.Append("_ => base.GetValue<TValue>(obj, index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteSetValue(string userTypeName, MemberData[] members)
        {
            _sb.Append("public override void SetValue<TValue>(").Append(userTypeName).Append(" obj, int index, TValue value)")
               .Indent().NewLine()
               .Append("switch (index)")
               .Indent().NewLine();

            foreach (var member in members)
            {
                _sb.Append("case ").Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append(")");

                // if memberType is enum, we need to figure out an underlying type and check on it
                var underlyingType = member.TypeSymbol.GetUnderlyingEnumTypeName();
                if (underlyingType is not null)
                {
                    _sb.Append(" || typeof(TValue) == typeof(").Append(underlyingType).Append(")");
                }
                _sb.Append(":").NewLine();

                _sb.Indent(withScope: false).Append("obj.").Append(member.Name).Append(" = UnsafePun<TValue, ").Append(member.Type).Append(">(value);").NewLine();
                _sb.Append("break;").NewLine().Outdent(withScope: false);
            }

            _sb.Outdent().NewLine()
               .Outdent().NewLine();
        }
    }

    private static string GetCustomTypeAccessorClassName(int num) => "DapperCustomTypeAccessor" + num;

    private static MemberData[] ConstructTypeMembers(ITypeSymbol typeSymbol)
    {
        var members = new List<MemberData>();
        int memberNumber = 0;

        foreach (var type in typeSymbol.GetMembers())
        {
            if (!CodeWriter.IsGettableInstanceMember(type, out var member) || !CodeWriter.IsSettableInstanceMember(type, out _))
            {
                // TODO not sure if we can use not settable or gettable field\property
                continue;
            }

            if (type is IPropertySymbol property)
            {
                members.Add(new()
                {
                    Name = property.Name,
                    Type = member.ToDisplayString(),
                    TypeSymbol = property.Type,
                    Number = memberNumber++,
                    IsNullable = property.Type.IsNullable()
                });
            }
            if (type is IFieldSymbol field)
            {
                members.Add(new()
                {
                    Name = field.Name,
                    Type = member.ToDisplayString(),
                    TypeSymbol = field.Type,
                    Number = memberNumber++,
                    IsNullable = field.NullableAnnotation == NullableAnnotation.Annotated
                });
            }
        }

#pragma warning disable IDE0305 // Simplify collection initialization
        return members.ToArray();
#pragma warning restore IDE0305 // Simplify collection initialization
    }

    [DebuggerDisplay("{TypeSymbol} {Name}")]
    struct MemberData
    {
        public int Number;
        public bool IsNullable;
        public string Name;
        public string Type;
        public ITypeSymbol TypeSymbol;
    }

    sealed class SourceStateByTypeComparer : IEqualityComparer<SourceState>
    {
        public static readonly SourceStateByTypeComparer Instance = new();

        public bool Equals(SourceState x, SourceState y) => SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType);
        public int GetHashCode(SourceState obj) => SymbolEqualityComparer.Default.GetHashCode(obj.ParameterType);
    }
}

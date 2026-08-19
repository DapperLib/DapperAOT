using Dapper.CodeAnalysis.Abstractions;
using Dapper.CodeAnalysis.Model;
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
        // note the cached values are all plain data (see ModelShapeTests): symbols must be
        // fully projected during parse, and the raw Compilation must not feed the output step
        var nodes = context.SyntaxProvider.CreateSyntaxProvider(PreFilter, Parse)
                    .Where(x => x is not null)
                    .Select((x, _) => x!);
        var env = context.CompilationProvider.Select(static (c, _) => CreateEnvironment(c));
        var combined = env.Combine(nodes.Collect());
        context.RegisterImplementationSourceOutput(combined, Generate);
    }

    private static GenerationEnvironment CreateEnvironment(Compilation compilation)
        => new(
            allowUnsafe: compilation.Options is CSharpCompilationOptions cSharp && cSharp.AllowUnsafe,
            assemblyName: compilation.AssemblyName,
            hasInterceptsLocationAttribute: PreGeneratedCodeWriter.HasInterceptsLocationAttribute(compilation));

    private bool PreFilter(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is InvocationExpressionSyntax invocation && invocation.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression.ToString() == "TypeAccessor" && (memberAccess.Name.ToString() is "CreateAccessor" or "CreateDataReader");
        }

        return false;
    }

    private TypeAccessorModel? Parse(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
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

        return new TypeAccessorModel(
            new LocationSnapshot(loc!),
            CodeWriter.GetTypeName(parameterType!),
            isCollection: Inspection.IsCollectionType(parameterType, out _),
            isPrimitive: Inspection.IsPrimitiveType(parameterType),
            members: ConstructTypeMembers(parameterType!),
            method: ProjectMethod(op.TargetMethod));

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

    private static ForwarderMethod ProjectMethod(IMethodSymbol method)
    {
        var args = method.Parameters;
        var parameters = new ForwarderParameter[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            bool isTypeAccessor = arg.Type is INamedTypeSymbol { IsGenericType: true, Arity: 1, Name: "TypeAccessor", ContainingType: null, ContainingNamespace: { Name: "Dapper", ContainingNamespace.IsGlobalNamespace: true } };
            parameters[i] = new ForwarderParameter(AppendedForm(arg.Type), arg.Name, isTypeAccessor);
        }
        return new ForwarderMethod(AppendedForm(method.ReturnType), AppendedForm(method.ContainingType), method.Name,
            new EquatableArray<ForwarderParameter>(parameters));

        // the string CodeWriter.Append(ITypeSymbol) would have produced
        static string AppendedForm(ITypeSymbol type) => type.IsAnonymousType
            ? type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            : CodeWriter.GetTypeName(type);
    }

    private void Generate(SourceProductionContext context, (GenerationEnvironment Env, ImmutableArray<TypeAccessorModel> Nodes) state)
    {
        if (!IsGenerateInputValid(ref context, state.Nodes))
        {
            Log?.Invoke(DiagnosticSeverity.Hidden, $"Generate input for '{nameof(TypeAccessorInterceptorGenerator)}' does not allow generation.");
            return;
        }

        var codeWriter = new CodeWriter();
        var sb = new TypeAccessorInterceptorCodeWriter(codeWriter);

        sb.WriteFileHeader(state.Env.AllowUnsafe);
        sb.WriteInterceptorsClass(() =>
        {
            int typeCounter = -1, methodCounter = 0;
            foreach (var group in state.Nodes.GroupBy(x => x.ParameterTypeName, StringComparer.Ordinal))
            {
                typeCounter++;

                var first = group.First();

                // not allowing collections
                if (first.IsCollection)
                {
                    ReportDiagnosticInUsages(Diagnostics.TypeAccessorCollectionTypeNotAllowed);
                    continue;
                }

                // not allowing primitives
                if (first.IsPrimitive)
                {
                    ReportDiagnosticInUsages(Diagnostics.TypeAccessorPrimitiveTypeNotAllowed);
                    continue;
                }

                var typeSymbolName = group.Key;
                var members = first.Members;
                if (members.Length == 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeAccessorMembersNotParsed, null));
                    continue;
                }

                foreach (var methodGroup in group.GroupBy(x => x.Method))
                {
                    foreach (var usage in methodGroup)
                    {
                        sb.WriteInterceptorsLocationAttribute(usage.Location);
                    }
                    sb.WriteMethodForwarder(methodGroup.Key, typeCounter, ref methodCounter);
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
                        context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, usage.Location.AsLocation()));
                    }
                }
            }
        });

        var preGenerator = new PreGeneratedCodeWriter(codeWriter, state.Env.HasInterceptsLocationAttribute);
        preGenerator.Write(IncludedGeneration.InterceptsLocationAttribute);

        context.AddSource((state.Env.AssemblyName ?? "package") + ".generated.cs", sb.GetSourceText());
    }

    private static bool IsGenerateInputValid(ref SourceProductionContext ctx, ImmutableArray<TypeAccessorModel> nodes)
    {
        if (nodes.IsDefaultOrEmpty)
        {
            // TODO report diagnostics
            return false;
        }

        return true;
    }

    [DebuggerDisplay("code: '{_sb.ToString()}'")]
    readonly struct TypeAccessorInterceptorCodeWriter
    {
        readonly CodeWriter _sb = new();
        public TypeAccessorInterceptorCodeWriter(CodeWriter codeWriter)
        {
            _sb = codeWriter;
        }

        public void WriteFileHeader(bool allowUnsafe)
        {
            if (allowUnsafe)
            {
                _sb.Append("#nullable enable").NewLine()
                    .Append("#pragma warning disable IDE0078 // unnecessary suppression is necessary").NewLine()
                    .Append("#pragma warning disable CS9270 // SDK-dependent change to interceptors usage").NewLine();
            }
        }

        public void WriteInterceptorsClass(Action innerWriter)
        {
            _sb.Append("namespace ").Append(FeatureKeys.CodegenNamespace)
                .Append(" // interceptors must be in a known namespace").Indent().NewLine()
                .Append("file static class DapperTypeAccessorGeneratedInterceptors").Indent().NewLine();
            innerWriter();
            _sb.Outdent().Outdent();
        }

        public void WriteInterceptorsLocationAttribute(in LocationSnapshot location)
        {
            _sb.Append("[global::System.Runtime.CompilerServices.InterceptsLocationAttribute(")
                .AppendVerbatimLiteral(location.Path).Append(", ").Append(location.StartLine + 1).Append(", ").Append(location.StartChar + 1).Append(")]")
                .NewLine();
        }

        public void WriteMethodForwarder(in ForwarderMethod method, int customTypeNum, ref int methodNumber)
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
                if (arg.IsTypeAccessorParam)
                {
                    _sb.Append(" ?? ").Append(GetCustomTypeAccessorClassName(customTypeNum)).Append(".Instance");
                }
                i++;
            }
            _sb.Append(");").Outdent(false).NewLine().NewLine();
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

        public void WriteTryIndex(string userTypeName, EquatableArray<AccessorMember> members)
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

        public void WriteGetName(string userTypeName, EquatableArray<AccessorMember> members)
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

        public void WriteIndexer(string userTypeName, EquatableArray<AccessorMember> members)
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

        public void WriteIsNullable(EquatableArray<AccessorMember> members)
        {
            _sb.Append("public override bool IsNullable(int index) => index switch")
               .Indent().NewLine();

            var strBuilder = new StringBuilder();
            foreach (var item in members)
            {
                if (item.IsNullable) strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => true,").NewLine();
            }

            strBuilder.Clear();
            foreach (var item in members)
            {
                if (!item.IsNullable) strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => false,").NewLine();
            }

            _sb.Append("_ => base.IsNullable(index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteIsNull(string userTypeName, EquatableArray<AccessorMember> members)
        {
            _sb.Append("public override bool IsNull(").Append(userTypeName).Append(" obj, int index) => index switch")
               .Indent().NewLine();

            var strBuilder = new StringBuilder();
            foreach (var item in members)
            {
                if (!item.IsNullable) strBuilder.Append(item.Number).Append(" or ");
            }
            if (strBuilder.Length > 0)
            {
                strBuilder.Length -= 4;
                _sb.Append(strBuilder.ToString()).Append(" => false,").NewLine();
            }

            foreach (var member in members)
            {
                if (!member.IsNullable) continue;
                if (member.IsDBNull)
                {
                    // if member is of type DBNull, then it is always null => simply return true
                    _sb.Append(member.Number).Append(" => true,").NewLine();
                    continue;
                }

                _sb.Append(member.Number).Append(" => obj.").Append(member.Name).Append(" is null");
                if (member.IsSystemObject)
                {
                    _sb.Append(" or global::System.DBNull");
                }
                _sb.Append(",").NewLine();
            }

            _sb.Append("_ => base.IsNull(obj, index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteGetType(EquatableArray<AccessorMember> members)
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

        public void WriteGetValue(string userTypeName, EquatableArray<AccessorMember> members)
        {
            _sb.Append("public override TValue GetValue<TValue>(").Append(userTypeName).Append(" obj, int index) => index switch")
                .Indent().NewLine();

            foreach (var member in members)
            {
                _sb.Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append(")");

                // if memberType is enum, we need to figure out an underlying type and check on it
                var underlyingType = member.UnderlyingEnumTypeName;
                if (underlyingType is not null)
                {
                    _sb.Append(" || typeof(TValue) == typeof(").Append(underlyingType).Append(")");
                }

                _sb.Append(" => UnsafePun<").Append(member.Type).Append(", TValue>(obj.").Append(member.Name).Append("),").NewLine();
            }

            _sb.Append("_ => base.GetValue<TValue>(obj, index)")
               .Outdent().Append(";").NewLine();
        }

        public void WriteSetValue(string userTypeName, EquatableArray<AccessorMember> members)
        {
            _sb.Append("public override void SetValue<TValue>(").Append(userTypeName).Append(" obj, int index, TValue value)")
               .Indent().NewLine()
               .Append("switch (index)")
               .Indent().NewLine();

            foreach (var member in members)
            {
                _sb.Append("case ").Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append(")");

                // if memberType is enum, we need to figure out an underlying type and check on it
                var underlyingType = member.UnderlyingEnumTypeName;
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

    private static EquatableArray<AccessorMember> ConstructTypeMembers(ITypeSymbol typeSymbol)
    {
        var members = new List<AccessorMember>();
        int memberNumber = 0;
        HashSet<string> seenNames = new(StringComparer.Ordinal);

        var tier = typeSymbol;
        while (tier is not null and not IErrorTypeSymbol)
        {
            foreach (var type in tier.GetMembers())
            {
                if (!CodeWriter.IsGettableInstanceMember(type, out var member) || !CodeWriter.IsSettableInstanceMember(type, out _))
                {
                    continue;
                }

                // skip members already seen at a more-derived level (handles `new` shadowing)
                if (!seenNames.Add(type.Name)) continue;

                if (type is IPropertySymbol property)
                {
                    members.Add(Create(property.Name, member, property.Type, property.Type.IsNullable()));
                }
                if (type is IFieldSymbol field)
                {
                    members.Add(Create(field.Name, member, field.Type, field.NullableAnnotation == NullableAnnotation.Annotated));
                }

                AccessorMember Create(string name, ITypeSymbol displayType, ITypeSymbol memberType, bool isNullable)
                    => new(memberNumber++, isNullable, name, displayType.ToDisplayString(),
                        isDbNull: IsDBNull(memberType),
                        isSystemObject: memberType.IsSystemObject(),
                        underlyingEnumTypeName: memberType.GetUnderlyingEnumTypeName());
            }

            tier = tier.BaseType;
        }

        return new EquatableArray<AccessorMember>(members.ToArray());

        static bool IsDBNull(ITypeSymbol typeSymbol)
        {
            return typeSymbol.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true
                && typeSymbol.ContainingNamespace.Name == "System"
                && typeSymbol.Name == "DBNull";
        }
    }
}

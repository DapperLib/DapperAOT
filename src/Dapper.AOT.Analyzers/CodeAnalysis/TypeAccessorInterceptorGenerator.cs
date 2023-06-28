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
using System.Reflection.Metadata.Ecma335;

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
                return memberAccess.Expression.ToString() == "TypeAccessor" && memberAccess.Name.ToString() == "CreateAccessor";
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
                int typeCounter = -1;
                foreach (var group in state.Nodes.GroupBy(x => x, SourceStateByTypeComparer.Instance))
                {
                    typeCounter++;

                    var typeSymbol = group.Key.ParameterType;
                    var usages = group.Select(x => x.Location);

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

                    var typeSymbolName = typeSymbol!.ToDisplayString();
                    var members = ConstructTypeMembers(typeSymbol!);
                    if (members.Length == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.TypeAccessorMembersNotParsed, null));
                        continue;
                    }

                    foreach (var location in usages)
                    {
                        sb.WriteInterceptorsLocationAttribute(location);
                        sb.WriteTypeAccessorCreateReaderMethod(typeCounter);
                    }

                    var accessorSb = new CustomTypeAccessorClassCodeWriter(codeWriter);
                    accessorSb.WriteClass(typeCounter, typeSymbolName, () =>
                    {
                        accessorSb.WriteMemberCount(members.Length);
                        accessorSb.WriteTryIndex(typeSymbolName, members);
                        accessorSb.WriteGetName(typeSymbolName, members);
                        accessorSb.WriteIndexer(typeSymbolName, members);
                        accessorSb.WriteIsNullable(members);
                        accessorSb.WriteGetType(members);
                        accessorSb.WriteGetValue(typeSymbolName, members);
                        accessorSb.WriteSetValue(typeSymbolName, members);
                    });

                    void ReportDiagnosticInUsages(DiagnosticDescriptor diagnosticDescriptor)
                    {
                        foreach (var usage in usages!)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, usage));
                        }
                    }
                }
            });

            context.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", sb.GetSourceText());
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

            public SourceText GetSourceText() => SourceText.From(_sb.ToString(), Encoding.UTF8);
        }

        [DebuggerDisplay("code: '{_sb.ToString()}'")]
        sealed class CustomTypeAccessorClassCodeWriter
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
                _sb.Append("public override int? TryIndex(string name, bool exact = false) => name switch")
                   .Indent().NewLine();

                foreach (var member in members)
                {
                    _sb.Append("nameof(").Append($"{userTypeName}.{member.Name}").Append(") => ").Append(member.Number).Append(",").NewLine();
                }

                _sb.Append("_ => base.TryIndex(name, exact)")
                   .Outdent().Append(";").NewLine();
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
                    _sb.Append("case ").Append(member.Number).Append(": => obj.")
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
                    // TODO! important: we need to support integers for enums, using the correct underlying type. i.e:
                    // 3 when typeof(TValue) == typeof(SomeEnum) || typeof(TValue) == typeof(int) => UnsafePun<SomeEnum, TValue>(obj.Foo),

                    _sb.Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append(")")
                       .Append(" => UnsafePun<").Append(member.Type).Append(", TValue>(obj.").Append(member.Name).Append("),").NewLine();
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
                    // TODO! we need to support integers for enums, using the correct underlying type
                    // case 3 when typeof(TValue) == typeof(SomeEnum) || typeof(TValue) == typeof(int):

                    _sb.Append("case ").Append(member.Number).Append(" when typeof(TValue) == typeof(").Append(member.Type).Append("):").NewLine();
                    _sb.Indent(withScope: false).Append("obj.").Append(member.Name).Append(" = UnsafePun<TValue, ").Append(member.Type).Append(">(value);").NewLine();
                    _sb.Append("break;").NewLine().Outdent(withScope: false);
                }

                _sb.Outdent().NewLine()
                   .Outdent().NewLine();
            }
        }

        private static string GetCustomTypeAccessorClassName(int num) => "DapperCustomTypeAccessor" + num;

        private MemberData[] ConstructTypeMembers(ITypeSymbol typeSymbol)
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
                        Number = memberNumber++,
                        IsNullable = property.NullableAnnotation == NullableAnnotation.Annotated
                    });
                }
                if (type is IFieldSymbol field)
                {
                    members.Add(new()
                    {
                        Name = field.Name,
                        Type = member.ToDisplayString(),
                        Number = memberNumber++,
                        IsNullable = field.NullableAnnotation == NullableAnnotation.Annotated
                    });
                }
            }

            return members.ToArray();
        }

        struct MemberData
        {
            public int Number;
            public bool IsNullable;
            public string Name;
            public string Type;
        }

        sealed class SourceStateByTypeComparer : IEqualityComparer<SourceState>
        {
            public static readonly SourceStateByTypeComparer Instance = new();

            public bool Equals(SourceState x, SourceState y) => SymbolEqualityComparer.Default.Equals(x.ParameterType, y.ParameterType);
            public int GetHashCode(SourceState obj) => SymbolEqualityComparer.Default.GetHashCode(obj.ParameterType);
        }
    }
}

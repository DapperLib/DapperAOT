using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Dapper.TestCommon;

internal static class RoslynTestHelpers
{
    internal static readonly CSharpParseOptions ParseOptionsLatestLangVer = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Latest)
        .WithPreprocessorSymbols(new string[] {
#if NETFRAMEWORK
        "NETFRAMEWORK",
#endif
#if NET40_OR_GREATER
        "NET40_OR_GREATER",
#endif
#if NET48_OR_GREATER
        "NET48_OR_GREATER",
#endif
#if NET6_0_OR_GREATER
        "NET6_0_OR_GREATER",
#endif
#if NET7_0_OR_GREATER
        "NET7_0_OR_GREATER",
#endif
#if DEBUG
        "DEBUG",
#endif
#if RELEASE
        "RELEASE",
#endif
    })
    .WithFeatures(new[] { DapperInterceptorGenerator.FeatureKeys.InterceptorsPreviewNamespacePair });

    public static Compilation CreateCompilation(string source, string name, string fileName)
       => CSharpCompilation.Create(name,
           syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, ParseOptionsLatestLangVer).WithFilePath(fileName) },
           references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
#if !NET48
                   MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Data").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                   MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).Assembly.Location),
#endif
                   MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(System.Data.SqlClient.SqlConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Microsoft.Data.SqlClient.SqlConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(OracleConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ValueTask<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DapperAotExtensions).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlMapper).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableList<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableArray<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Span<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IgnoreDataMemberAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SqlMapper).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IValidatableObject).Assembly.Location),
           },
           options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true));
}
using Dapper.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace Dapper.AOT.Test
{
    public partial class SamplesTests : GeneratorTestBase
    {
        public SamplesTests(ITestOutputHelper log) : base(log) { }

        public static IEnumerable<object[]> GetFiles() =>
            from path in Directory.GetFiles("Samples", "*.cs")
            where path.EndsWith(".input.cs", StringComparison.OrdinalIgnoreCase)
            select new object[] { path };

        [Theory, MemberData(nameof(GetFiles))]
        public void Run(string path)
        {
            var intputPath = File.ReadAllText(path);
            var outputPath = Regex.Replace(path, @"\.input\.cs$", ".output.cs", RegexOptions.IgnoreCase);
            var expected = File.Exists(outputPath) ? File.ReadAllText(outputPath) : "";
            var sb = new StringBuilder();
            var result = Execute<CommandGenerator>(intputPath, sb, fileName: path,
                initializer: g => g.DefaultOutputFileName = Path.GetFileName(outputPath));
            Assert.Single(result.Result.GeneratedTrees);
            var generated = Assert.Single(Assert.Single(result.Result.Results).GeneratedSources);

            string? code = generated.SourceText?.ToString();
            Log(code);
            sb.AppendLine().AppendLine(code);
            Assert.Equal(expected.Trim(), sb.ToString().Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
    }
}

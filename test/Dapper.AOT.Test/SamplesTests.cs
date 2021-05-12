using Dapper.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Dapper.AOT.Test
{
    public partial class SamplesTests : GeneratorTestBase
    {
        public SamplesTests(ITestOutputHelper log) : base(log) { }

        public static IEnumerable<object[]> GetFiles() =>
            from path in Directory.GetFiles("Samples", "*.cs", SearchOption.AllDirectories)
            where path.EndsWith(".input.cs", StringComparison.OrdinalIgnoreCase)
            select new object[] { path };

        [Theory, MemberData(nameof(GetFiles))]
        public void Run(string path)
        {
            var intputPath = File.ReadAllText(path);
#if NET48   // lots of deltas
            var outputPath = Regex.Replace(path, @"\.input\.cs$", ".output.netfx.cs", RegexOptions.IgnoreCase);
#else
            var outputPath = Regex.Replace(path, @"\.input\.cs$", ".output.cs", RegexOptions.IgnoreCase);
#endif
            var expected = File.Exists(outputPath) ? File.ReadAllText(outputPath) : "";
            var sb = new StringBuilder();
            var result = Execute<CommandGenerator>(intputPath, sb, fileName: path, initializer: g =>
            {
                g.DefaultOutputFileName = Path.GetFileName(outputPath);
                g.ReportVersion = false;
                g.Log += (severity, message) => Log($"{severity}: {message}");
            });
            Assert.Single(result.Result.GeneratedTrees);
            var generated = Assert.Single(Assert.Single(result.Result.Results).GeneratedSources);

            string? code = generated.SourceText?.ToString();
#if DEBUG
            Log(code);
#endif
            sb.AppendLine().AppendLine(code);

            var actual = sb.ToString();
            try // automatically overwrite test output, for git tracking
            {
                if (GetOriginCodeLocation() is string originFile
                    && Path.GetDirectoryName(originFile) is string originFolder)
                {
                    outputPath = Path.Combine(originFolder, outputPath);
                    File.WriteAllText(outputPath, actual);

                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }

            Assert.Equal(expected.Trim(), actual.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        static string? GetOriginCodeLocation([CallerFilePath] string? path = null) => path;
    }
}

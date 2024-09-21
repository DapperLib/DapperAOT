using Dapper.AOT.Test.Helpers;
using Dapper.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.AOT.Test;

public class InterceptorTests : GeneratorTestBase
{
    public InterceptorTests(ITestOutputHelper log) : base(log) { }

    public static IEnumerable<object[]> GetFiles()
    {
        var currentNetVersion = TestFramework.DetermineNetVersion();

        foreach (var path in Directory.GetFiles("Interceptors", "*.cs", SearchOption.AllDirectories))
        {
            if (path.EndsWith(".input.cs", StringComparison.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(path);
                var fileNetVersionStr = fileName.Split('.')[1];
                if (TestFramework.NetVersions.Contains(fileNetVersionStr))
                {
                    // it has to be the same or greater version
                    var fileNetVersion = (TestFramework.NET)Enum.Parse(typeof(TestFramework.NET), fileNetVersionStr);
                    if (currentNetVersion < fileNetVersion)
                    {
                        // skip if current version is lower than specified in the input file name
                        continue;
                    }
                }

                yield return new object[] { path };
            }
        }

        yield break;
    }

    [Theory, MemberData(nameof(GetFiles))]
    public void Test(string path)
    {
        var sourceText = File.ReadAllText(path);
#if NET48   // lots of deltas
        var outputCodePath = Regex.Replace(path, @"\.input\.cs$", ".output.netfx.cs", RegexOptions.IgnoreCase);
#else
        var outputCodePath = Regex.Replace(path, @"\.input\.cs$", ".output.cs", RegexOptions.IgnoreCase);
#endif
        var outputBuildPath = Path.ChangeExtension(outputCodePath, "txt");

        var expectedCode = File.Exists(outputCodePath) ? File.ReadAllText(outputCodePath) : "";
        var expectedBuildOutput = File.Exists(outputBuildPath) ? File.ReadAllText(outputBuildPath) : "";

        var sb = new StringBuilder();
        var result = Execute<DapperInterceptorGenerator>(sourceText, sb, fileName: path, initializer: g =>
        {
            g.Log += message => Log(message);
        });

        var results = Assert.Single(result.Result.Results);
        string actualCode = results.GeneratedSources.Any() ? results.GeneratedSources.Single().SourceText?.ToString() ?? "" : "";

        var buildOutput = sb.ToString();
        try // automatically overwrite test output, for git tracking
        {
            if (GetOriginCodeLocation() is string originFile
                && Path.GetDirectoryName(originFile) is string originFolder)
            {
                outputCodePath = Path.Combine(originFolder, outputCodePath);
                outputBuildPath = Path.ChangeExtension(outputCodePath, "txt");
                if (string.IsNullOrWhiteSpace(buildOutput))
                {
                    try { File.Delete(outputBuildPath); } catch { }
                }
                else
                {
                    File.WriteAllText(outputBuildPath, buildOutput);
                }
                if (string.IsNullOrWhiteSpace(actualCode))
                {
                    try { File.Delete(outputCodePath); } catch { }
                }
                else
                {
                    File.WriteAllText(outputCodePath, actualCode);
                }
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(expectedCode.Trim(), actualCode.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        Assert.Equal(expectedBuildOutput.Trim(), buildOutput.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }
}

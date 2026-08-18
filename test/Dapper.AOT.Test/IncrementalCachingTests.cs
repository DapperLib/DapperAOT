using Dapper.AOT.Test.TestCommon;
using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Xunit;

namespace Dapper.AOT.Test;

/// <summary>
/// Proves the incremental pipeline actually caches: the whole point of the plain-data model
/// (see ModelShapeTests) is that an edit which does not change any Dapper call-site must not
/// re-run the output step - and a real edit must. Both directions are asserted, so this test
/// is known to be able to fail.
/// </summary>
public class IncrementalCachingTests
{
    const string DapperUsage = """
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]

        public static class Program
        {
            public static Customer Get(DbConnection connection, int id)
                => connection.QueryFirst<Customer>("select Id, Name from Customers where Id = @id", new { id });
        }
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
        """;

    const string Unrelated = """
        public static class Unrelated
        {
            public static int Value => 1;
        }
        """;

    static (GeneratorDriver Driver, Compilation Compilation) Setup()
    {
        var compilation = RoslynTestHelpers.CreateCompilation(DapperUsage, "incremental_test", "Usage.cs")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(Unrelated, RoslynTestHelpers.ParseOptionsLatestLangVer).WithFilePath("Unrelated.cs"));
        var generator = new DapperInterceptorGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: RoslynTestHelpers.ParseOptionsLatestLangVer,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        return (driver, compilation);
    }

    static string GetOutput(GeneratorDriver driver)
        => driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();

    static Compilation ReplaceTree(Compilation compilation, string filePath, string newSource)
    {
        var oldTree = compilation.SyntaxTrees.Single(t => t.FilePath == filePath);
        var newTree = CSharpSyntaxTree.ParseText(newSource, RoslynTestHelpers.ParseOptionsLatestLangVer).WithFilePath(filePath);
        return compilation.ReplaceSyntaxTree(oldTree, newTree);
    }

    [Fact]
    public void UnrelatedEditDoesNotRerunTheOutputStep()
    {
        var (driver, compilation) = Setup();
        var baseline = GetOutput(driver);
        Assert.Contains("QueryFirst", baseline); // sanity: we generated something real

        compilation = ReplaceTree(compilation, "Unrelated.cs", Unrelated.Replace("=> 1", "=> 2"));
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var outputs = result.TrackedOutputSteps.SelectMany(kv => kv.Value).SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.True(
            output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
            $"output step re-ran: {output.Reason}"));
        Assert.Equal(baseline, GetOutput(driver));
    }

    [Fact] // the sharpest case: the *same file* is edited, so Parse re-runs and produces new
    // state instances - only the states' structural equality stops the output re-running
    public void SameFileEditBelowTheCallSiteDoesNotRerunTheOutputStep()
    {
        var (driver, compilation) = Setup();
        var baseline = GetOutput(driver);

        // note: below the call-site, so the interceptor location does not move
        compilation = ReplaceTree(compilation, "Usage.cs", DapperUsage + "// trailing comment");
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var outputs = result.TrackedOutputSteps.SelectMany(kv => kv.Value).SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputs);
        Assert.All(outputs, output => Assert.True(
            output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
            $"output step re-ran: {output.Reason}"));
        Assert.Equal(baseline, GetOutput(driver));
    }

    [Fact]
    public void RealEditDoesRerunTheOutputStep()
    {
        var (driver, compilation) = Setup();
        var baseline = GetOutput(driver);

        // note the SQL text itself flows through as a runtime argument, so the edit must be
        // one that changes the generated *shape*: add a bindable member to the row type
        compilation = ReplaceTree(compilation, "Usage.cs", DapperUsage.Replace("public int Id { get; set; }", "public int Id { get; set; } public int Extra { get; set; }"));
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult().Results.Single();
        var outputs = result.TrackedOutputSteps.SelectMany(kv => kv.Value).SelectMany(step => step.Outputs).ToArray();
        Assert.NotEmpty(outputs);
        Assert.Contains(outputs, output => output.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New);
        Assert.NotEqual(baseline, GetOutput(driver));
    }
}

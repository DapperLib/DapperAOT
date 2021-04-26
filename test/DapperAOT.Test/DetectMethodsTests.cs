using DapperAOT.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace DapperAOT.Test
{
    public partial class DetectMethodsTests : GeneratorTestBase
    {
        public DetectMethodsTests(ITestOutputHelper log) : base(log) { }

        //partial void ShouldIgnoreThis();
        //public partial int ShouldDetectThis();
        //public partial int ShouldDetectThis() => 42;
        [Fact]
        public void CanDetectMethods()
        {
            var result = Execute<CommandGenerator>(@"
using Dapper;
class Foo {
    partial void ShouldIgnoreThis_NoAccessibility();
    [Command]
    public partial int ShouldDetectThis();

    public partial int ShouldIgnoreThis_NoAttribute();

    [Command]
    public partial int ShouldIgnoreThis_HasImplementation();
    public partial int ShouldIgnoreThis_HasImplementation() => 42;
}
");
            Assert.Empty(result.Diagnostics);
            Assert.Single(result.Result.GeneratedTrees);
            var generated = Assert.Single(Assert.Single(result.Result.Results).GeneratedSources);
            Log(generated.SourceText);
        }
    }
}

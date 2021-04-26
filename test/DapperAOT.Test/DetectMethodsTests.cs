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
using System.Data;
class Foo {
    partial void ShouldIgnoreThis_NoAccessibility(DbConnection connection, string region);
    [Command(""select * from Customers where Region = @region"")]
    public partial int ShouldDetectThis(DbConnection connection, string region);

    public partial int ShouldIgnoreThis_NoAttribute(DbConnection connection, string region);

    [Command]
    public partial int ShouldIgnoreThis_HasImplementation(DbConnection connection, string region);
    public partial int ShouldIgnoreThis_HasImplementation(DbConnection connection, string region) => 42;
}

namespace X.Y.Z
{
    partial class A<TRandom>
    {
        partial class B
        {
            [Command(""select * from Customers where Region = @region"")]
            public partial int ShouldDetectThisInB(DbConnection connection, string region);
        }
    }
}
namespace X.Y.Z
{
    partial class A<TRandom>
    {
        partial class B
        {
           [Command(""select * from Customers where Region = @region"")]
            public partial int ShouldAlsoDetectThisInB(DbConnection connection, string region);
        }
    }
}
");
            Assert.Empty(result.Diagnostics);
            Assert.Single(result.Result.GeneratedTrees);
            var generated = Assert.Single(Assert.Single(result.Result.Results).GeneratedSources);
            Log(generated.SourceText);
        }
    }
}

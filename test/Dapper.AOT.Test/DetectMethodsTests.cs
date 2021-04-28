using Dapper.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Dapper.AOT.Test
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
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
partial class Foo {
    partial void ShouldIgnoreThis_NoAccessibility(string region);
    [Command(""select * from Customers where Region = @region"", CommandType = CommandType.Text)]
    public partial int? ShouldDetectThis(DbConnection connection, string region);

    public partial int ShouldIgnoreThis_NoAttribute(string region);

    [Command]
    public partial int ShouldIgnoreThis_HasImplementation(string region);
    public partial int ShouldIgnoreThis_HasImplementation(string region) => 42;
}

namespace X.Y.Z
{
    partial class A<TRandom>
    {
        partial class B
        {
            [Command(""select * from Customers where Region = @region"")]
            public virtual partial Customer ViaDapper(string region, SqlConnection c);

            [Command(""select * from Customers where Region = @region"")]
            public new static partial Customer ViaOracle(string region, OracleConnection c);
        }
    }
    class Customer {}
}
namespace X.Y.Z
{
    partial class A<TRandom>
    {
        partial class B
        {
           [Command(""select * from Customers where Region = @region"")]
            public partial int ShouldAlsoDetectThisInB(string region);
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

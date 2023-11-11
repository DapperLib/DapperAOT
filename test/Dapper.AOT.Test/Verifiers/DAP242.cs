using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public partial class DAP242 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_PlusInt() => VerifyCSharpConcatenatedTest("""
        int id = 1;
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id|});
    """);

    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_PlusString() => VerifyCSharpConcatenatedTest("""
        string id = "1";
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id|});
    """);

    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_MultiVariablesConcat() => VerifyCSharpConcatenatedTest("""
        int id = 1;
        string name = "dima";
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id + " and Name = " + name|});
    """);

    [Fact]
    public Task ConcatenatedStringDetection_LocalVariableUsage() => VerifyCSharpConcatenatedTest("""
        int id = 1;
        var sqlQuery = "select Id from Customers where Id = " + id;
        _ = connection.Query<int>({|#0:sqlQuery|});
    """);

    [Fact]
    public Task ConcatenatedStringDetection_StringFormat() => VerifyCSharpConcatenatedTest("""
        int id = 1;
        _ = connection.Query<int>({|#0:string.Format("select Id from Customers where Id = {0}", id)|});
    """);

    private Task VerifyCSharpConcatenatedTest(string methodCode) => CSVerifyAsync($$"""
        using Dapper;
        using System.Data;
        using System.Data.Common;
        
        [DapperAot]
        class HasUnusedParameter
        {
            void SomeMethod(DbConnection connection)
            {
                {{methodCode}}
            }
        }
    """, DefaultConfig, [ Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0) ]);
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;

namespace Dapper.AOT.Test.Verifiers;

public partial class DAP241 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task CSharpInterpolatedStringDetection_DirectUsage() => VerifyCSharpInterpolatedTest("""
        int id = 1;
        _ = connection.Query<int>({|#0:$"select Id from Customers where Id = {id}"|});
    """);

    [Fact]
    public Task InterpolatedRawStringLiteralsDetection_DirectUsage() => VerifyCSharpInterpolatedTest(""""
        int id = 1;
        _ = connection.Query<int>({|#0:$"""
            select Id from Customers where Id = {id}
        """|});
    """");

    [Fact]
    public Task InterpolatedRawStringLiteralsDetection_LocalVariableUsage() => VerifyCSharpInterpolatedTest(""""
        int id = 1;
        var sqlQuery = $"""
            select Id from Customers where Id = {id}
        """;
        _ = connection.Query<int>({|#0:sqlQuery|});
    """");

    [Fact]
    public Task InterpolatedStringDetection_LocalVariableUsage() => VerifyCSharpInterpolatedTest("""
        int id = 1;
        var sqlQuery = $"select Id from Customers where Id = {id}";
        _ = connection.Query<int>({|#0:sqlQuery|});
    """);

    private Task VerifyCSharpInterpolatedTest(string methodCode) => CSVerifyAsync($$"""
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
    """, DefaultConfig, [ Diagnostic(Diagnostics.InterpolatedStringSqlExpression).WithLocation(0) ]);
}
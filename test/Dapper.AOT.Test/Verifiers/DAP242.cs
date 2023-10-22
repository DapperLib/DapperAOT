using Dapper.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP242 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_PlusInt() => Test("""
        int id = 1;
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id|});
    """, Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0));

    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_PlusString() => Test("""
        string id = "1";
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id|});
    """, Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0));

    [Fact]
    public Task ConcatenatedStringDetection_DirectUsage_MultiVariablesConcat() => Test("""
        int id = 1;
        string name = "dima";
        _ = connection.Query<int>({|#0:"select Id from Customers where Id = " + id + " and Name = " + name|});
    """, Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0));

    [Fact]
    public Task ConcatenatedStringDetection_LocalVariableUsage() => Test("""
        int id = 1;
        var sqlQuery = "select Id from Customers where Id = " + id;
        _ = connection.Query<int>({|#0:sqlQuery|});
    """, Diagnostic(Diagnostics.ConcatenatedStringSqlExpression).WithLocation(0));

    private Task Test(string methodCode, params DiagnosticResult[] expected) => CSVerifyAsync($$"""
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
    """, DefaultConfig, expected);
}
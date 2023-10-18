using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP241 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task InterpolatedStringDetection_DirectUsage() => CSVerifyAsync(""""
        using Dapper;
        using System.Data;
        using System.Data.Common;
        
        [DapperAot]
        class HasUnusedParameter
        {
            void SomeMethod(DbConnection connection)
            {
                int id = 1;
                _ = connection.Query<int>({|#0:$"select Id from Customers where Id = {id}"|});
            }
        }
    """", 
        DefaultConfig,
        [
            Diagnostic(Diagnostics.InterpolatedStringSqlExpression).WithLocation(0),
        ]
    );

    [Fact]
    public Task InterpolatedStringDetection_LocalVariableUsage() => CSVerifyAsync(""""
        using Dapper;
        using System.Data;
        using System.Data.Common;
        
        [DapperAot]
        class HasUnusedParameter
        {
            void SomeMethod(DbConnection connection)
            {
                int id = 1;
                var sqlQuery = $"select Id from Customers where Id = {id}";
                _ = connection.Query<int>({|#0:sqlQuery|});
            }
        }
    """",
        DefaultConfig,
        [
            Diagnostic(Diagnostics.InterpolatedStringSqlExpression).WithLocation(0),
        ]
    );
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP028 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UseQueryAsList() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Linq;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                // as extension methods
                _ = conn.Query<SomeType>("storedproc").{|#0:ToList|}();
                
                // explicit call (location test)
                _ = Enumerable.{|#1:ToList|}(conn.Query<SomeType>("storedproc"));;

                // full generics
                _ = conn.Query<SomeType>("storedproc").{|#2:ToList<SomeType>|}();

                // fully qualified explicit call (location test)
                _ = global::System.Linq.Enumerable.{|#3:ToList|}(conn.Query<SomeType>("storedproc"));
                
                // valid usage
                _ = conn.Query<SomeType>("storedproc").AsList();
            }
        }
        class SomeType { public int Id {get;set;} }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UseQueryAsList).WithLocation(0),
            Diagnostic(Diagnostics.UseQueryAsList).WithLocation(1),
            Diagnostic(Diagnostics.UseQueryAsList).WithLocation(2),
            Diagnostic(Diagnostics.UseQueryAsList).WithLocation(3),
    ]);

}
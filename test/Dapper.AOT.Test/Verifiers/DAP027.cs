using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP027 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UseSingleRowQuery() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Linq;

        [DapperAot(true)]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                // as extension methods
                _ = conn.Query<SomeType>("storedproc").{|#0:First|}();
                _ = conn.Query<SomeType>("storedproc").{|#1:Single|}();
                _ = conn.Query<SomeType>("storedproc").{|#2:FirstOrDefault|}();
                _ = conn.Query<SomeType>("storedproc").{|#3:SingleOrDefault|}();

                // fully generics
                _ = conn.Query<SomeType>("storedproc").{|#4:First<SomeType>|}();

                // explicit call (location test)
                _ = Enumerable.{|#5:First|}(conn.Query<SomeType>("storedproc"));;

                // fully qualified explicit call (location test)
                _ = global::System.Linq.Enumerable.{|#6:Single|}(conn.Query<SomeType>("storedproc"));
                
                // valid usage
                _ = conn.QueryFirst<SomeType>("storedproc");
                _ = conn.QuerySingle<SomeType>("storedproc");
                _ = conn.QueryFirstOrDefault<SomeType>("storedproc");
                _ = conn.QuerySingleOrDefault<SomeType>("storedproc");
            }
        }
        class SomeType { public int Id {get;set;} }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(0).WithArguments("QueryFirst", "First"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(1).WithArguments("QuerySingle", "Single"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(2).WithArguments("QueryFirstOrDefault", "FirstOrDefault"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(3).WithArguments("QuerySingleOrDefault", "SingleOrDefault"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(4).WithArguments("QueryFirst", "First"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(5).WithArguments("QueryFirst", "First"),
            Diagnostic(Diagnostics.UseSingleRowQuery).WithLocation(6).WithArguments("QuerySingle", "Single"),
        ]);

}
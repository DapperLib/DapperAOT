using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP023 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DuplicateRowCount() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class TwoRowCounts
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#1:RowCount|}]
            public int X {get;set;}

            [{|#0:RowCount|}] // dup is primary
            public int Y {get;set;}
        }

        [DapperAot]
        class SingleRowCount
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [RowCount]
            public int X {get;set;}
        
            public int Y {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateRowCount).WithLocation(0).WithLocation(1).WithArguments("X", "Y"),
    ]);
}
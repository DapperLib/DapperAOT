using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP032 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task RowCountHintDuplicated() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeTypeWithDuplicatedRowCountHint
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#1:RowCountHint|}]
            public int X {get;set;}

            [{|#0:RowCountHint|}]
            public int Y {get;set;}
        }

        [DapperAot]
        class SomeTypeWithSingleRowCountHint
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);
        
            [RowCountHint]
            public int X {get;set;}

            public int Y {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RowCountHintDuplicated).WithLocation(0).WithLocation(1).WithArguments("Y"),
    ]);
}
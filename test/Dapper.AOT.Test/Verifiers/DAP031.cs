using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP031 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task RowCountHintShouldNotSpecifyValue() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeTypeWithMemberHintWithValue
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#0:RowCountHint(42)|}]
            public int X {get;set;}
        }

        [DapperAot]
        class SomeTypeWithMemberHintWithoutValue
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);
        
            [RowCountHint]
            public int X {get;set;}
        }

        [DapperAot]
        class SomeTypeWithoutMemberHint
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            public int X {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RowCountHintShouldNotSpecifyValue).WithLocation(0),
    ]);
}
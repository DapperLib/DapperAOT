using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP029 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task RowCountHintRedundant() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeTypeWithMemberHint
        {
            [{|#0:RowCountHint(42)|}]
            void WithMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            void WithoutMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            [RowCountHint]
            public int X {get;set;}
        }

        class SomeTypeWithMemberHintButNoAot
        {
            [RowCountHint(42)]
            void WithMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            void WithoutMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            [RowCountHint]
            public int X {get;set;}
        }

        [DapperAot]
        class SomeTypeWithoutMemberHint
        {
            [RowCountHint(42)]
            void WithMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            void WithoutMethodHint(DbConnection conn)
                => conn.Execute("someproc", this);

            public int X {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RowCountHintRedundant).WithLocation(0).WithArguments("X"),
    ]);
}
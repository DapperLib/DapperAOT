using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP030 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task RowCountHintInvalidValue() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeType
        {
            [{|#0:RowCountHint(0)|}]
            void ZeroHint(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#1:RowCountHint(-42)|}]
            void NegativeHint(DbConnection conn)
            => conn.Execute("someproc", this);

            [{|#2:RowCountHint|}]
            void HintWithoutValue(DbConnection conn)
            => conn.Execute("someproc", this);

            [RowCountHint(42)]
            void ValidHint(DbConnection conn)
                => conn.Execute("someproc", this);
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RowCountHintInvalidValue).WithLocation(0),
            Diagnostic(Diagnostics.RowCountHintInvalidValue).WithLocation(1),
            Diagnostic(Diagnostics.RowCountHintInvalidValue).WithLocation(2)
    ]);
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP024 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task RowCountDbValue() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class BothAttribsOnOneMember
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#1:RowCount|}, {|#0:DbValue|}] // dbval is primary
            public int X {get;set;}
        }

        [DapperAot]
        class AttribsOnDifferentMembers
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [RowCount]
            public int X {get;set;}
            [DbValue]
            public int Y {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RowCountDbValue).WithLocation(0).WithLocation(1).WithArguments("X"),
    ]);
}
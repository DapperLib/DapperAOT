using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP022 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DuplicateReturn() => CSVerifyAsync("""
        using Dapper;
        using System.Data;
        using System.Data.Common;

        [DapperAot]
        class TwoReturns
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [{|#1:DbValue(Direction = ParameterDirection.ReturnValue)|}]
            public int X {get;set;}

            [{|#0:DbValue(Direction = ParameterDirection.ReturnValue)|}] // dup is primary
            public int Y {get;set;}
        }

        [DapperAot]
        class SingleReturn
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            [DbValue(Direction = ParameterDirection.ReturnValue)]
            public int X {get;set;}
        
            public int Y {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateReturn).WithLocation(0).WithLocation(1).WithArguments("X", "Y"),
    ]);
}
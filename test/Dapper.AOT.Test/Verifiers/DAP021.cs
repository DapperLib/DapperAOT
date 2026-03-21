using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP021 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DuplicateParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class Data
        {
            void SomeMethod(DbConnection conn)
                => conn.Execute("someproc", this);

            public int {|#1:Id|} {get;set;}

            [{|#0:DbValue(Name = "Id")|}] // location should prefer the attrib when exists; dup is primary
            public int OtherId {get;set;}

            [{|#3:DbValue(Name = "X")|}]
            public int Y {get;set;}

            [{|#2:DbValue(Name = "X")|}] // dup is primary
            public int Z {get;set;}

            public int {|#5:casing|} {get;set;}
            public int {|#4:CASING|} {get;set;} // dup is primary

            public int fixedCase {get;set;}
            [DbValue(Name = "AnotherName")]
            public int FixedCase {get;set;}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateParameter).WithLocation(0).WithLocation(1).WithArguments("Id", "OtherId", "Id"),
            Diagnostic(Diagnostics.DuplicateParameter).WithLocation(2).WithLocation(3).WithArguments("Y", "Z", "X"),
            Diagnostic(Diagnostics.DuplicateParameter).WithLocation(4).WithLocation(5).WithArguments("casing", "CASING", "CASING"),
    ]);
}
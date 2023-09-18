using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP225 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task FromMultiTableMissingAlias() => SqlVerifyAsync("""
        select a.Id
        from A a
        inner join {|#0:B|} on a.X = a.Id

        select a.Id
        from A a
        inner join B b on b.X = a.Id

        select Id
        from Users
        where Id in (
            select a.Id
            from A a
            inner join B b on b.X = a.Id)

        select Id
        from Users
        where Id in (
            select a.Id
            from A a
            inner join {|#1:B|} on a.X = a.Id)

        delete {|#2:Users|}
        from Users u
        inner join B b on b.X = u.Id

        delete u
        from Users u
        inner join B b on b.X = u.Id

        update {|#3:Users|}
        set u.Id = 42
        from Users u
        inner join B b on b.X = u.Id

        update u
        set u.Id = 42
        from Users u
        inner join B b on b.X = u.Id
        """, Diagnostic(Diagnostics.FromMultiTableMissingAlias).WithLocation(0),
            Diagnostic(Diagnostics.FromMultiTableMissingAlias).WithLocation(1),
            Diagnostic(Diagnostics.FromMultiTableMissingAlias).WithLocation(2),
            Diagnostic(Diagnostics.FromMultiTableMissingAlias).WithLocation(3));
    
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP222 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectAssignAndRead() => SqlVerifyAsync("""
        declare @id int;
        {|#0:select @id = Id, Name from Users|};
        select @id;
        """, Diagnostic(Diagnostics.SelectAssignAndRead).WithLocation(0).WithArguments("Name"));
    
}
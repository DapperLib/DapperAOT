using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP221 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectDuplicateColumnName() => SqlVerifyAsync("""
        select Id, Name, {|#0:FirstName + ' ' + Surname as [Name]|} from Users

        select Id, Name, FirstName + ' ' + Surname as [FullName] from Users
        """, SqlAnalysis.SqlParseInputFlags.ValidateSelectNames, Diagnostic(Diagnostics.SelectDuplicateColumnName).WithLocation(0).WithArguments("Name"));
    
}
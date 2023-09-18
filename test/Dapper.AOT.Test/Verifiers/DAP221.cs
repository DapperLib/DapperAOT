using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.SqlAnalysis.TSqlProcessor;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP221 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectDuplicateColumnName() => SqlVerifyAsync("""
        select Id, Name, {|#0:FirstName + ' ' + Surname as [Name]|} from Users

        select Id, Name, FirstName + ' ' + Surname as [FullName] from Users
        """, ModeFlags.ValidateSelectNames, Diagnostic(Diagnostics.SelectDuplicateColumnName).WithLocation(0).WithArguments("Name"));
    
}
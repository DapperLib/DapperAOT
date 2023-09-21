using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP220 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SelectEmptyColumnName() => SqlVerifyAsync("""
        select Id, {|#0:Credit - Debit|} from Accounts

        select Id, Credit - Debit as [Balance] from Accounts
        """, SqlAnalysis.SqlParseInputFlags.ValidateSelectNames, Diagnostic(Diagnostics.SelectEmptyColumnName).WithLocation(0).WithArguments(1));
    
}
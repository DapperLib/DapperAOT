using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Diagnostics = Dapper.CodeAnalysis.DapperAnalyzer.Diagnostics;
namespace Dapper.AOT.Test.Verifiers;

public class DAP245 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UseGoWithoutDelimiter() => SqlVerifyAsync("""
        INSERT {|#0:GO|} ({|#1:GO|}) VALUES (42);
        SELECT {|#2:GO|} FROM {|#3:GO|};
        """,
        Diagnostic(Diagnostics.DangerousNonDelimitedIdentifier).WithLocation(0).WithArguments("GO"),
        Diagnostic(Diagnostics.DangerousNonDelimitedIdentifier).WithLocation(1).WithArguments("GO"),
        Diagnostic(Diagnostics.DangerousNonDelimitedIdentifier).WithLocation(2).WithArguments("GO"),
        Diagnostic(Diagnostics.DangerousNonDelimitedIdentifier).WithLocation(3).WithArguments("GO"));

    [Fact]
    public Task UseGoWithDelimited() => SqlVerifyAsync("""
        INSERT [GO] ([GO]) VALUES (42);
        SELECT [GO] FROM [GO];
        """);

}
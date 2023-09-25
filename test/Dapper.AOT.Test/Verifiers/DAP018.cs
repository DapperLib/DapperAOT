using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP018 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task SqlParametersNotDetected() => CSVerifyAsync(""""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeType
        {
            public void NoArgsOrLocals(DbConnection connection)
                => connection.Execute("""
                insert SomeTable(Id) values (42)
                """, {|#0:new { id = 42 }|});

            public void UsesArgs(DbConnection connection)
                => connection.Execute("""
                insert SomeTable(Id) values (@id)
                """, new { id = 42 });

            public void UsesThingsCloseToArgs(DbConnection connection)
                => connection.Execute("""
                declare @id2 int = 14;
                insert SomeTable(Id, Age) values (@@id, @id2)
                """, {|#1:new { id = 42 }|});
        }
        """", DefaultConfig, [
            Diagnostic(Diagnostics.SqlParametersNotDetected).WithLocation(0),
            Diagnostic(Diagnostics.SqlParametersNotDetected).WithLocation(1),
    ]);
}
using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP043 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UseColumnAttributeNotSpecified() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.ComponentModel.DataAnnotations.Schema;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<MyType>("def");
            }
        }
        class MyType
        {
            [{|#0:Column("ColumnAttrPropName")|}]
            public int PropName { get; set; }
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.UseColumnAttributeNotSpecified).WithLocation(0),
        ]
    );

}
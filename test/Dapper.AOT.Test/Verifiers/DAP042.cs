using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP042 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ParameterNameOverrideConflict() => CSVerifyAsync("""
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
            [UseColumnAttribute]
            [{|#0:DbValue(Name = "DbValuePropName")|}]
            [{|#1:Column("ColumnAttrPropName")|}]
            public int PropName { get; set; }
        }
        """,
        DefaultConfig,
        [
            Diagnostic(Diagnostics.ParameterNameOverrideConflict).WithLocation(0).WithLocation(1).WithArguments("DbValuePropName"),
        ]
    );

}
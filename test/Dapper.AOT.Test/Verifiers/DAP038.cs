using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP038 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ValueTypeSingleFirstOrDefaultUsage() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Threading.Tasks;

        [DapperAot(true)]
        class SomeCode
        {
            public async Task Foo(DbConnection conn)
            {
                // issue only affects value-type and {First|Single}OrDefault
                _ = conn.{|#0:QueryFirstOrDefault<SomeStruct>|}("storedproc");
                _ = conn.{|#1:QuerySingleOrDefault<SomeStruct>|}("storedproc");
                // check async
                _ = await conn.{|#2:QueryFirstOrDefaultAsync<SomeStruct>|}("storedproc");

                _ = conn.QueryFirstOrDefault<SomeStruct?>("storedproc");
                _ = conn.QuerySingleOrDefault<SomeStruct?>("storedproc");
                _ = await conn.QuerySingleOrDefaultAsync<SomeStruct?>("storedproc");

                _ = conn.QueryFirst<SomeStruct>("storedproc");
                _ = conn.QuerySingle<SomeStruct>("storedproc");
                _ = conn.QueryFirstOrDefault<SomeClass>("storedproc");
                _ = conn.QuerySingleOrDefault<SomeClass>("storedproc");
                _ = conn.QueryFirst<SomeClass>("storedproc");
                _ = conn.QuerySingle<SomeClass>("storedproc");
                

            }
        }
        class SomeClass { public int Id {get;set;} }
        struct SomeStruct { public int Id {get;set;} }
        """, DefaultConfig, [
        Diagnostic(Diagnostics.ValueTypeSingleFirstOrDefaultUsage).WithLocation(0).WithArguments("SomeStruct", "QueryFirstOrDefault"),
        Diagnostic(Diagnostics.ValueTypeSingleFirstOrDefaultUsage).WithLocation(1).WithArguments("SomeStruct", "QuerySingleOrDefault"),
        Diagnostic(Diagnostics.ValueTypeSingleFirstOrDefaultUsage).WithLocation(2).WithArguments("SomeStruct", "QueryFirstOrDefaultAsync"),
        ]);

}
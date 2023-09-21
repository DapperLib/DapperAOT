using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP013 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task DapperAotTupleResults() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot(true)]
        class SomeCode
        {
            [BindTupleByName(false)]
            public void EnabledWithoutNames(DbConnection conn)
            {
                _ = conn.{|#0:Query<(string,int)>|}("somesql");
            }
            [BindTupleByName(true)]
            public void EnabledWithNames(DbConnection conn)
            {
                _ = conn.{|#1:Query<(string s,int a)>|}("somesql");
            }
            [DapperAot(false)]
            public void UseVanillaDapper(DbConnection conn)
            {
                _ = conn.Query<(string,int)>("somesql");
            }
            public void UseRecordStruct(DbConnection conn)
            {
                _ = conn.Query<MyData>("somesql");
            }
            public readonly record struct MyData(string name, int id);
        }
        namespace System.Runtime.CompilerServices
        {
            static file class IsExternalInit {}
        }
        """, DefaultConfig, [Diagnostic(Diagnostics.DapperAotTupleResults).WithLocation(0),
        Diagnostic(Diagnostics.DapperAotTupleResults).WithLocation(1)]);
}
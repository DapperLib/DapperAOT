using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP036 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task ConstructorAmbiguous() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;
        using System.Runtime.Serialization;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                _ = conn.Query<NoConstructors>("storedproc");
                _ = conn.Query<SingleImplicit>("storedproc");
                _ = conn.Query<MultipleImplicit>("storedproc");

                _ = conn.Query<RecordClass>("storedproc");
                _ = conn.Query<RecordStruct>("storedproc");
                _ = conn.Query<ReadOnlyRecordStruct>("storedproc");
            }

            [DapperAot(false)]
            public void NoAotMode(DbConnection conn)
            {
                // not a problem in legacy mode
                _ = conn.Query<MultipleImplicit>("storedproc");
            }
        }
        class NoConstructors { public int Id {get;set;} }
        [System.Serializable]
        class SingleImplicit
        {
            public string A {get;}
            public SingleImplicit(string a) => A = a;
            public SingleImplicit(SingleImplicit b) {}
            public SingleImplicit(SerializationInfo info, StreamingContext ctx) {}
        }
        class MultipleImplicit
        {
            public int A {get;set;}
            public {|#0:MultipleImplicit|}(int a) {}
            public MultipleImplicit(string b) {}
            public MultipleImplicit(MultipleImplicit c) {}
        }
        record class RecordClass(int a);
        record struct RecordStruct(int a);
        readonly record struct ReadOnlyRecordStruct(int a);

        namespace System.Runtime.CompilerServices
        {
            static file class IsExternalInit {}
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.ConstructorAmbiguous).WithLocation(0).WithArguments("MultipleImplicit"),
    ]);

}
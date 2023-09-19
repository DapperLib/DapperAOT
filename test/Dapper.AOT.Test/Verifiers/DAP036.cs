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
        }
        class NoConstructors {}
        class SingleImplicit
        {
            public SingleImplicit(string a) {}
            public SingleImplicit(SingleImplicit b) {}
        }
        class MultipleImplicit
        {
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
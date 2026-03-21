using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP037 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UserTypeNoSettableMembersFound() => CSVerifyAsync(""""
        using Dapper;
        using System.Collections.Generic;
        using System.Data.Common;
        using System.Runtime.Serialization;

        [DapperAot]
        class SomeCode
        {
            public void Foo(DbConnection conn)
            {
                var args = new NoSettable(32); // important that this is fine as an arg
                const string sql = """
                    select Id, Name
                    from SomeTable
                    where Id = @b
                    """;

                _ = conn.{|#0:Query<NoSettable>|}(sql, args);
                _ = conn.Query<SimpleType>(sql, args);
                _ = conn.Query<IntOnlyType>(sql, args);
                _ = conn.Query<RecordType>(sql, args);
                _ = conn.Query<ReadWriteField>(sql, args);
                _ = conn.{|#1:Query<ReadOnlyField>|}(sql, args);
                _ = conn.Query<HazImplicitConstructor>(sql, args);
                _ = conn.Query<HazExplicitConstructor>(sql, args);
                _ = conn.Query<sbyte[]>(sql, args);
                _ = conn.Query<byte[]>(sql, args);
                _ = conn.{|#2:Query<int[]>|}(sql, args);
            }
        }

        class NoSettable
        {
            public NoSettable(int a) => B = a;
            public int B {get;}
        }
        class SimpleType
        {
            public int B {get;set;}
        }
        class IntOnlyType
        {
            public int B {get;init;}
        }
        class ReadWriteField
        {
            public int B;
        }
        class ReadOnlyField
        {
            public readonly int B;
        }
        record class RecordType(int b);
        class HazImplicitConstructor
        {
            public HazImplicitConstructor(int b) => B = b;
            public int B {get;}
        }
        class HazExplicitConstructor
        {
            [ExplicitConstructor]
            public HazExplicitConstructor(int b) => B = b;
            public int B {get;}
        }
        namespace System.Runtime.CompilerServices
        {
            static file class IsExternalInit {}
        }
        """", DefaultConfig, [
            Diagnostic(Diagnostics.UserTypeNoSettableMembersFound).WithLocation(0).WithArguments("NoSettable"),
            Diagnostic(Diagnostics.UserTypeNoSettableMembersFound).WithLocation(1).WithArguments("ReadOnlyField"),
            Diagnostic(Diagnostics.UserTypeNoSettableMembersFound).WithLocation(2).WithArguments(""),
    ]);

}
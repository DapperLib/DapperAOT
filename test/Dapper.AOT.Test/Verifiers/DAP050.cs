using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP050 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UnconstructableResultType() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class SomeCode
        {
            void Queries(DbConnection conn)
            {
                // no accessible parameterless constructor (like EF's DbGeography)
                _ = conn.{|#0:Query<PrivateCtor>|}("somesql");
                // only a parameterized constructor whose parameter matches no member (like System.Data.Linq.Binary)
                _ = conn.{|#1:Query<BinaryLike>|}("somesql");
                // abstract types cannot be constructed at all
                _ = conn.{|#2:Query<AbstractType>|}("somesql");
                // fine: ordinary POCO
                _ = conn.Query<Poco>("somesql");
                // fine: constructor parameters correspond to members
                _ = conn.Query<UsableCtor>("somesql");
                // fine: structs need no constructor
                _ = conn.Query<SomeStruct>("somesql");
            }
        }
        public class PrivateCtor { private PrivateCtor() { } public int Value {get;set;} }
        public class BinaryLike { public BinaryLike(byte[] value) { } public int Length => 0; }
        public abstract class AbstractType { public int Value {get;set;} }
        public class Poco { public int Value {get;set;} }
        public class UsableCtor { public UsableCtor(int value) { Value = value; } public int Value { get; } }
        public struct SomeStruct { public int Value {get;set;} }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UnconstructableResultType).WithLocation(0).WithArguments("PrivateCtor"),
            Diagnostic(Diagnostics.UnconstructableResultType).WithLocation(1).WithArguments("BinaryLike"),
            Diagnostic(Diagnostics.UserTypeNoSettableMembersFound).WithLocation(1).WithArguments("BinaryLike"),
            Diagnostic(Diagnostics.UnconstructableResultType).WithLocation(2).WithArguments("AbstractType"),
    ]);
}

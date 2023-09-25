using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP017 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task NonPublicType() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        internal class NestedInternal
        {
            [DapperAot]
            class AotEnabled
            {
                void ArgsA(DbConnection conn, OuterPublic args) => conn.Execute("somesql", args);
                void ArgsB(DbConnection conn, OuterInternal args) => conn.Execute("somesql", args);
                void ArgsC(DbConnection conn, NestedInternal.InnerPublic args) => conn.Execute("somesql", args);
                void ArgsD(DbConnection conn, NestedInternal.InnerPrivate args) => conn.Execute("somesql", {|#0:args|});
                void ArgsE(DbConnection conn, NestedInternal.InnerPrivate.InnerInnerPublic args) => conn.Execute("somesql", {|#1:args|});
                void ArgsF(DbConnection conn, NestedInternal.InnerProtected args) => conn.Execute("somesql", {|#2:args|});
                void ArgsG(DbConnection conn, NestedInternal.InnerProtectedInternal args) => conn.Execute("somesql", args);
                void ArgsH(DbConnection conn, NestedInternal.InnerPrivateProtected args) => conn.Execute("somesql", {|#3:args|});

                void QueryA(DbConnection conn) => _ = conn.Query<OuterPublic>("somesql");
                void QueryB(DbConnection conn) => _ = conn.Query<OuterInternal>("somesql");
                void QueryC(DbConnection conn) => _ = conn.Query<NestedInternal.InnerPublic>("somesql");
                void QueryD(DbConnection conn) => _ = conn.{|#4:Query<NestedInternal.InnerPrivate>|}("somesql");
                void QueryE(DbConnection conn) => _ = conn.{|#5:Query<NestedInternal.InnerPrivate.InnerInnerPublic>|}("somesql");
                void QueryF(DbConnection conn) => _ = conn.{|#6:Query<InnerProtected>|}("somesql");
                void QueryG(DbConnection conn) => _ = conn.Query<InnerProtectedInternal>("somesql");
                void QueryH(DbConnection conn) => _ = conn.{|#7:Query<InnerPrivateProtected>|}("somesql");

            }
            [DapperAot(false)]
            class AotDisabled
            {
                void ArgsA(DbConnection conn, OuterPublic args) => conn.Execute("somesql", args);
                void ArgsB(DbConnection conn, OuterInternal args) => conn.Execute("somesql", args);
                void ArgsC(DbConnection conn, NestedInternal.InnerPublic args) => conn.Execute("somesql", args);
                void ArgsD(DbConnection conn, NestedInternal.InnerPrivate args) => conn.Execute("somesql", args);
                void ArgsE(DbConnection conn, NestedInternal.InnerPrivate.InnerInnerPublic args) => conn.Execute("somesql", args);
                void ArgsF(DbConnection conn, NestedInternal.InnerProtected args) => conn.Execute("somesql", args);
                void ArgsG(DbConnection conn, NestedInternal.InnerProtectedInternal args) => conn.Execute("somesql", args);
                void ArgsH(DbConnection conn, NestedInternal.InnerPrivateProtected args) => conn.Execute("somesql", args);

                void QueryA(DbConnection conn) => _ = conn.Query<OuterPublic>("somesql");
                void QueryB(DbConnection conn) => _ = conn.Query<OuterInternal>("somesql");
                void QueryC(DbConnection conn) => _ = conn.Query<NestedInternal.InnerPublic>("somesql");
                void QueryD(DbConnection conn) => _ = conn.Query<NestedInternal.InnerPrivate>("somesql");
                void QueryE(DbConnection conn) => _ = conn.Query<NestedInternal.InnerPrivate.InnerInnerPublic>("somesql");
                void QueryF(DbConnection conn) => _ = conn.Query<InnerProtected>("somesql");
                void QueryG(DbConnection conn) => _ = conn.Query<InnerProtectedInternal>("somesql");
                void QueryH(DbConnection conn) => _ = conn.Query<InnerPrivateProtected>("somesql");
            }
            public class InnerPublic { public int Id {get;set;} }
            protected class InnerProtected {}
            protected internal class InnerProtectedInternal { public int Id {get;set;} }
            private protected class InnerPrivateProtected {}
            private class InnerPrivate
            {
                public class InnerInnerPublic {}
            }
        }

        public class OuterPublic { public int Id {get;set;} }
        internal class OuterInternal { public int Id {get;set;} }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.NonPublicType).WithLocation(0).WithArguments("NestedInternal.InnerPrivate", "private"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(1).WithArguments("NestedInternal.InnerPrivate", "private"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(2).WithArguments("NestedInternal.InnerProtected", "protected"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(3).WithArguments("NestedInternal.InnerPrivateProtected", "private protected"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(4).WithArguments("NestedInternal.InnerPrivate", "private"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(5).WithArguments("NestedInternal.InnerPrivate", "private"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(6).WithArguments("NestedInternal.InnerProtected", "protected"),
            Diagnostic(Diagnostics.NonPublicType).WithLocation(7).WithArguments("NestedInternal.InnerPrivateProtected", "private protected"),
    ]);
}
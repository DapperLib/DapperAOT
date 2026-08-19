using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP016 : Verifier<DapperAnalyzer>
{
    [Fact]
    public Task UntypedParameter() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class GenericType<TX>
        {
            void WithTypeArg(DbConnection conn, TX args) => conn.Execute("somesql", {|#0:args|});
        }
        [DapperAot]
        class NonGenericType
        {
            void WithMethodArg<TY>(DbConnection conn, TY args) => conn.Execute("somesql", {|#1:args|});
            class InnerGenercType<TZ>
            {
                void WithInnerTypeArg(DbConnection conn, TZ args) => conn.Execute("somesql", {|#2:args|});
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.GenericTypeParameter).WithLocation(0).WithArguments("TX"),
            Diagnostic(Diagnostics.GenericTypeParameter).WithLocation(1).WithArguments("TY"),
            Diagnostic(Diagnostics.GenericTypeParameter).WithLocation(2).WithArguments("TZ")]);

    [Fact] // a member *type* that involves the container's type parameter is just as unusable
    public Task GenericTypeParameterViaMember() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        class GenericType<TX>
        {
            public class Nested { public int Id {get;set;} }

            // Nested means GenericType<TX>.Nested, which generated code cannot name
            void ViaAnonymousMember(DbConnection conn, Nested value) => conn.Execute("somesql", {|#0:new { value }|});
            void ViaPocoIsFine(DbConnection conn, Poco args) => conn.Execute("somesql", args);
        }
        class Poco { public int Id {get;set;} }
        """, DefaultConfig, [
            // note: this shape now gets the more specific DAP051 (nested-in-generic) guidance
            Diagnostic(Diagnostics.NestedInGenericType).WithLocation(0).WithArguments("GenericType<TX>.Nested", "GenericType<TX>")]);

}
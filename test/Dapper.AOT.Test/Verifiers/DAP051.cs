using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP051 : Verifier<DapperAnalyzer>
{
    [Fact] // the accidental shape: a generic-free DTO nested inside a generic class
    public Task NestedInGenericType() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [DapperAot]
        public class AnimalTests<TProvider>
        {
            class Dog { public int Age {get;set;} public string? Name {get;set;} }

            void ViaResult(DbConnection conn) => _ = conn.{|#0:Query<Dog>|}("somesql");
            void ViaParameter(DbConnection conn, Dog args) => conn.Execute("somesql", {|#1:args|});
            void ViaMember(DbConnection conn, Dog value) => conn.Execute("somesql", {|#2:new { value }|});

            // genuinely generic usage stays DAP016
            void TrueGeneric<T>(DbConnection conn, T args) => conn.Execute("somesql", {|#3:args|});
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.NestedInGenericType).WithLocation(0).WithArguments("AnimalTests<TProvider>.Dog", "AnimalTests<TProvider>"),
            Diagnostic(Diagnostics.NestedInGenericType).WithLocation(1).WithArguments("AnimalTests<TProvider>.Dog", "AnimalTests<TProvider>"),
            Diagnostic(Diagnostics.NestedInGenericType).WithLocation(2).WithArguments("AnimalTests<TProvider>.Dog", "AnimalTests<TProvider>"),
            Diagnostic(Diagnostics.GenericTypeParameter).WithLocation(3).WithArguments("T"),
    ]);
}

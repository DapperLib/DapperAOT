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

}
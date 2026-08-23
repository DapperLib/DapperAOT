using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP053 : Verifier<DapperAnalyzer>
{
    [Fact] // a runtime registration the generator cannot see, in both spellings
    public Task RuntimeRegistrationIsInvisible() => CSVerifyAsync("""
        using Dapper;
        using System;
        using System.Data;

        [module: DapperAot]

        public struct LocalDate { public int Year; }

        public sealed class LocalDateHandler : SqlMapper.TypeHandler<LocalDate>
        {
            public override void SetValue(IDbDataParameter parameter, LocalDate value) { }
            public override LocalDate Parse(object value) => default;
        }

        public static class Startup
        {
            public static void Register()
            {
                {|#0:SqlMapper.AddTypeHandler(new LocalDateHandler())|};
                {|#1:SqlMapper.AddTypeHandler(typeof(LocalDate), new LocalDateHandler())|};
            }
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.RuntimeTypeHandlerRegistration).WithLocation(0).WithArguments("LocalDate", "LocalDateHandler"),
            Diagnostic(Diagnostics.RuntimeTypeHandlerRegistration).WithLocation(1).WithArguments("LocalDate", "LocalDateHandler"),
    ]);

    [Fact] // declared via the attribute: the runtime call is redundant, not wrong, so: quiet
    public Task DeclaredHandlerIsNotReported() => CSVerifyAsync("""
        using Dapper;
        using System;
        using System.Data;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(LocalDateHandler))]

        public struct LocalDate { public int Year; }

        public sealed class LocalDateHandler : SqlMapper.TypeHandler<LocalDate>
        {
            public override void SetValue(IDbDataParameter parameter, LocalDate value) { }
            public override LocalDate Parse(object value) => default;
        }

        public static class Startup
        {
            public static void Register() => SqlMapper.AddTypeHandler(new LocalDateHandler());
        }
        """, DefaultConfig, []);

    [Fact] // no Dapper.AOT in play: vanilla-only code is behaving correctly
    public Task NotReportedWithoutDapperAot() => CSVerifyAsync("""
        using Dapper;
        using System;
        using System.Data;

        public struct LocalDate { public int Year; }

        public sealed class LocalDateHandler : SqlMapper.TypeHandler<LocalDate>
        {
            public override void SetValue(IDbDataParameter parameter, LocalDate value) { }
            public override LocalDate Parse(object value) => default;
        }

        public static class Startup
        {
            public static void Register() => SqlMapper.AddTypeHandler(new LocalDateHandler());
        }
        """, DefaultConfig, []);
}

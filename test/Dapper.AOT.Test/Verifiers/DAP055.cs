using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP055 : Verifier<DapperAnalyzer>
{
    private const string Handlers = """

        public struct LocalDate { public int Year; }

        public sealed class FirstHandler : DbValueHandler<LocalDate>
        {
            protected override void SetValueCore(DbParameter parameter, LocalDate value) { }
            protected override LocalDate Parse(object? value) => default;
        }

        public sealed class SecondHandler : DbValueHandler<LocalDate>
        {
            protected override void SetValueCore(DbParameter parameter, LocalDate value) { }
            protected override LocalDate Parse(object? value) => default;
        }
        """;

    [Fact] // two different handlers for one type: no correct resolution, so name the loser
    public Task ConflictingRegistrations() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        [module: {|#0:TypeHandler(typeof(LocalDate), typeof(SecondHandler))|}]
        """ + Handlers, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateTypeHandler).WithLocation(0)
                .WithArguments("LocalDate", "FirstHandler", "SecondHandler"),
    ]);

    [Fact] // ...and the report survives the two scopes being mixed
    public Task ConflictAcrossModuleAndAssemblyScope() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        [assembly: {|#0:TypeHandler(typeof(LocalDate), typeof(SecondHandler))|}]
        """ + Handlers, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateTypeHandler).WithLocation(0)
                .WithArguments("LocalDate", "FirstHandler", "SecondHandler"),
    ]);

    [Fact] // an exact repeat is harmless - same handler, same outcome - so stay quiet
    public Task IdenticalRepeatIsNotReported() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        """ + Handlers, DefaultConfig, []);

    [Fact] // a dropped duplicate is not also graded for usability: one message, not two
    public Task DuplicateIsNotAlsoReportedAsUnusable() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        [module: {|#0:TypeHandler(typeof(LocalDate), typeof(NotAHandler))|}]

        public sealed class NotAHandler { }
        """ + Handlers, DefaultConfig, [
            Diagnostic(Diagnostics.DuplicateTypeHandler).WithLocation(0)
                .WithArguments("LocalDate", "FirstHandler", "NotAHandler"),
    ]);

    [Fact] // distinct types are not duplicates
    public Task DistinctTypesAreQuiet() => CSVerifyAsync("""
        using Dapper;
        using System.Data.Common;

        [module: DapperAot]
        [module: TypeHandler(typeof(LocalDate), typeof(FirstHandler))]
        [module: TypeHandler(typeof(Money), typeof(MoneyHandler))]

        public struct Money { public decimal Amount; }

        public sealed class MoneyHandler : DbValueHandler<Money>
        {
            protected override void SetValueCore(DbParameter parameter, Money value) { }
            protected override Money Parse(object? value) => default;
        }
        """ + Handlers, DefaultConfig, []);
}

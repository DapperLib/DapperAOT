using Dapper.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using static Dapper.CodeAnalysis.DapperAnalyzer;

namespace Dapper.AOT.Test.Verifiers;

public class DAP054 : Verifier<DapperAnalyzer>
{
    // module attributes must precede everything else, so each case declares its own header
    private const string Header = """
        using Dapper;
        using System;
        using System.Data;
        using System.Data.Common;

        [module: DapperAot]
        """;

    private const string Types = """

        public struct LocalDate { public int Year; }
        public struct Money { public decimal Amount; }
        """;

    [Fact] // the handler implements neither contract - the commonest way to get this wrong
    public Task NeitherContract() => CSVerifyAsync(Header + """
        [module: {|#0:TypeHandler(typeof(LocalDate), typeof(NotAHandler))|}]
        """ + Types + """

        public sealed class NotAHandler { }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UnusableTypeHandler).WithLocation(0)
                .WithArguments("NotAHandler", "LocalDate", "it implements neither IDbValueHandler<T> nor SqlMapper.ITypeHandler"),
    ]);

    [Fact] // a real handler, registered against the wrong value type: name both sides
    public Task WrongValueType() => CSVerifyAsync(Header + """
        [module: {|#0:TypeHandler(typeof(Money), typeof(LocalDateHandler))|}]
        """ + Types + """

        public sealed class LocalDateHandler : DbValueHandler<LocalDate>
        {
            protected override void SetValueCore(DbParameter parameter, LocalDate value) { }
            protected override LocalDate Parse(object? value) => default;
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UnusableTypeHandler).WithLocation(0)
                .WithArguments("LocalDateHandler", "Money", "it handles 'LocalDate', not 'Money'"),
    ]);

    [Fact] // generated code has to construct it
    public Task NoPublicParameterlessConstructor() => CSVerifyAsync(Header + """
        [module: {|#0:TypeHandler(typeof(LocalDate), typeof(NeedsArgs))|}]
        """ + Types + """

        public sealed class NeedsArgs : DbValueHandler<LocalDate>
        {
            public NeedsArgs(int scale) { }
            protected override void SetValueCore(DbParameter parameter, LocalDate value) { }
            protected override LocalDate Parse(object? value) => default;
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UnusableTypeHandler).WithLocation(0)
                .WithArguments("NeedsArgs", "LocalDate", "it has no public parameterless constructor"),
    ]);

    [Fact] // abstract cannot be instantiated
    public Task Abstract() => CSVerifyAsync(Header + """
        [module: {|#0:TypeHandler(typeof(LocalDate), typeof(AbstractHandler))|}]
        """ + Types + """

        public abstract class AbstractHandler : DbValueHandler<LocalDate>
        {
            protected override LocalDate Parse(object? value) => default;
        }
        """, DefaultConfig, [
            Diagnostic(Diagnostics.UnusableTypeHandler).WithLocation(0)
                .WithArguments("AbstractHandler", "LocalDate", "it is abstract"),
    ]);

    [Fact] // both good shapes stay quiet: native, and vanilla-via-the-shim
    public Task UsableHandlersAreQuiet() => CSVerifyAsync(Header + """
        [module: TypeHandler(typeof(LocalDate), typeof(LocalDateHandler))]
        [module: TypeHandler(typeof(Money), typeof(MoneyHandler))]
        """ + Types + """

        public sealed class LocalDateHandler : DbValueHandler<LocalDate>
        {
            protected override void SetValueCore(DbParameter parameter, LocalDate value) { }
            protected override LocalDate Parse(object? value) => default;
        }

        public sealed class MoneyHandler : SqlMapper.TypeHandler<Money>
        {
            public override void SetValue(IDbDataParameter parameter, Money value) { }
            public override Money Parse(object value) => default;
        }
        """, DefaultConfig, []);
}

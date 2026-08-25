#nullable enable
using Dapper;
using System;
using System.Data;
using System.Data.Common;

[module: DapperAot]

// declarative registration: per-assembly, deterministic, and visible to the generator, unlike
// SqlMapper.AddTypeHandler. Non-generic on purpose - a generic attribute cannot be read by
// .NET Framework's GetCustomAttributes
[module: TypeHandler(typeof(LocalDate), typeof(LocalDateHandler))]
[module: TypeHandler(typeof(Money), typeof(MoneyHandler))]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // native handler (IDbValueHandler<T>) on the write and read paths
        _ = connection.Query<Appointment>("select * from Appointments where Day = @Day",
            new Appointment { Day = new LocalDate() });

        // a nullable member: null goes to SetNullValue, so a handler over a struct never sees a
        // null it cannot express
        _ = connection.Execute("update Appointments set MovedTo = @MovedTo where Day = @Day",
            new Appointment { Day = new LocalDate(), MovedTo = null });

        // vanilla Dapper handler, reached through the generated adapter
        _ = connection.Query<Invoice>("select * from Invoices where Total = @Total",
            new Invoice { Total = new Money() });

        // output parameters read back through the handler
        _ = connection.Execute("exec NextAppointment @Day out", new AppointmentOut());
    }
}

public struct LocalDate { public int Year, Month, Day; }
public struct Money { public decimal Amount; }

public class Appointment
{
    public LocalDate Day { get; set; }
    public LocalDate? MovedTo { get; set; }
}

public class AppointmentOut
{
    [DbValue(Direction = ParameterDirection.Output)]
    public LocalDate Day { get; set; }
}

public class Invoice
{
    public Money Total { get; set; }
}

// the new shape: the generator emits a single static and calls it directly.
// Tokenize runs once per column per query and its result is handed back to Parse for every
// row, so a per-column decision (here: which shape the provider gave us) is paid once
public sealed class LocalDateHandler : DbValueHandler<LocalDate>
{
    protected override void Configure(DbParameter parameter) => parameter.DbType = DbType.Date;
    protected override void SetValueCore(DbParameter parameter, LocalDate value)
        => parameter.Value = new DateTime(value.Year, value.Month, value.Day);

    public override int Tokenize(DbDataReader reader, int columnOffset)
        => reader.GetFieldType(columnOffset) == typeof(string) ? 1 : 0;

    public override LocalDate Parse(DbDataReader reader, int ordinal, int token)
        => Parse(token == 1 ? DateTime.Parse(reader.GetString(ordinal)) : reader.GetValue(ordinal));

    protected override LocalDate Parse(object? value)
    {
        var when = (DateTime)value!;
        return new LocalDate { Year = when.Year, Month = when.Month, Day = when.Day };
    }
}

// the old shape, written against vanilla Dapper: still usable, via the generated shim
public sealed class MoneyHandler : SqlMapper.TypeHandler<Money>
{
    public override void SetValue(IDbDataParameter parameter, Money value) => parameter.Value = value.Amount;
    public override Money Parse(object value) => new Money { Amount = (decimal)value };
}

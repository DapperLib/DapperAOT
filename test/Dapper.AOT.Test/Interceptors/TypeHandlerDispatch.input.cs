using Dapper;
using System.Data.Common;

[module: DapperAot]

public static class Foo
{
    static void SomeCode(DbConnection connection)
    {
        // an unrecognized member type defers to vanilla's decision procedure at execution
        // time: a runtime SqlMapper.AddTypeHandler registration binds via the handler, and
        // otherwise the value binds raw exactly as before (modern providers handle types
        // vanilla's map does not)
        _ = connection.Execute("insert Events (At, Name) values (@At, @Name)", new EventRow { At = new LocalDate { Year = 2026, Month = 8, Day = 20 }, Name = "x" });

        // reads resolve handlers through the flexible path; nothing changes in the emit here
        _ = connection.Query<EventRow>("select At, Name from Events");
    }

    public class LocalDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }
    public class EventRow
    {
        public LocalDate At { get; set; }
        public string Name { get; set; }
    }
}

using Dapper;
using System;
using System.Data.Common;

#nullable enable

[DapperAot]
class ShowRowReaderUsage
{
    public void DoSomething(DbDataReader reader)
    {
        var parser = reader.GetRowParser<HazNameId>();
        while (reader.Read())
        {
            var obj = parser(reader);
            Console.WriteLine($"{obj.Id}: {obj.Name}");
        }
    }

    public void DoSomethingSpecifyingConcreteType(DbDataReader reader)
    {
        var parser = reader.GetRowParser<HazNameId>(concreteType: typeof(HazNameId));
        while (reader.Read())
        {
            var obj = parser(reader);
            Console.WriteLine($"{obj.Id}: {obj.Name}");
        }
    }

    public void DoSomethingDynamic(DbDataReader reader)
    {
        var parser = reader.GetRowParser<dynamic>();
        while (reader.Read())
        {
            var obj = parser(reader);
            Console.WriteLine($"{obj.Id}: {obj.Name}");
        }
    }

    public void DoSomethingTypeBased(DbDataReader reader)
    {
        var parser = reader.GetRowParser(typeof(HazNameId));
        while (reader.Read())
        {
            var obj = (HazNameId)parser(reader);
            Console.WriteLine($"{obj.Id}: {obj.Name}");
        }
    }
}

public class HazNameId
{
    public string? Name { get; set; }
    public int Id { get; set; }
}
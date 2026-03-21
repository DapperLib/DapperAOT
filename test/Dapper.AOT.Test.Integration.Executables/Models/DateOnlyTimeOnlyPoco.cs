using System;

namespace Dapper.AOT.Test.Integration.Executables.Models;

public class DateOnlyTimeOnlyPoco
{
    public const string TableName = "dateOnlyTimeOnly";

    public static DateOnly SpecificDate => DateOnly.FromDateTime(new DateTime(year: 2022, month: 2, day: 2));
    public static TimeOnly SpecificTime => TimeOnly.FromDateTime(new DateTime(year: 2022, month: 1, day: 1, hour: 10, minute: 11, second: 12));
    
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
}
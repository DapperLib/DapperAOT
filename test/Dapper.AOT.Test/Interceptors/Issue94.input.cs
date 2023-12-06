using Dapper;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

#nullable enable

namespace TodosApi;

public static class Usage
{
    [DapperAot]
    public static async Task Foo(MyDbSource source)
    {
        _ = await source.QueryAsync<Todo>("this doesn't matter");
    }
}

public class MyDbSource // spoof DbDataSource
{
    public DbConnection GetConnection() => throw new NotImplementedException();
}
static class MyDbSourceExtensions
{
    [DapperAot]
    public static async Task<List<T>> QueryAsync<T>(this MyDbSource source, string sql, object? args = null)
    {
        using var conn = source.GetConnection();
        var results = await conn.QueryAsync<T>(sql, args);
        return results.AsList();
    }
}
internal sealed class Todo : IDataReaderMapper<Todo>, IValidatable
{
    bool IValidatable.RequiresServiceProvider => false;

    public int Id { get; set; }

    public string Title { get; set; } = default!;

    public DateTime? DueBy { get; set; } // subst from DateOnly for netfx

    public bool IsComplete { get; set; }

    public static Todo Map(DbDataReader dataReader)
    {
        return !dataReader.HasRows ? new() : new()
        {
            Id = dataReader.GetInt32(dataReader.GetOrdinal(nameof(Id))),
            Title = dataReader.GetString(dataReader.GetOrdinal(nameof(Title))),
            IsComplete = dataReader.GetBoolean(dataReader.GetOrdinal(nameof(IsComplete)))
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Title))
        {
            yield return new ValidationResult($"A value is required for {nameof(Title)}.", new[] { nameof(Title) });
            yield break;
        }
        if (DueBy.HasValue && DueBy.Value < DateTime.UtcNow) // subst from DateOnly for netfx
        {
            yield return new ValidationResult($"{nameof(DueBy)} cannot be in the past.", new[] { nameof(DueBy) });
        }
    }
}

internal interface IDataReaderMapper<T> {
    // removed for netfx
    // abstract static T Map(DbDataReader dataReader);
}

internal interface IValidatable : IValidatableObject
{
    bool RequiresServiceProvider { get; } // => false; // remove default impl for netfx
}
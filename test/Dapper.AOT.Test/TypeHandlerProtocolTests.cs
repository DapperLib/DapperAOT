using Dapper;
using System;
using System.Data;
using System.Data.Common;
using Xunit;

namespace Dapper.AOT.Test;

/// <summary>
/// Pins the contract that generated row-factories rely on when a member reads through a
/// <see cref="IDbValueHandler{T}"/>: <c>Tokenize</c> is a per-query, per-column decision whose
/// result reaches <c>Parse</c> for every row. The factory below is hand-written in the shape the
/// generator emits (see the TypeHandlerRegistration golden), so this test fails if either side
/// of that protocol drifts.
/// </summary>
public class TypeHandlerProtocolTests
{
    [Fact]
    public void TokenizeRunsOncePerColumn_AndItsTokenReachesEveryRow()
    {
        var handler = new CountingHandler();
        var factory = new HandRolledFactory(handler);

        using var reader = CreateReader(("Day", typeof(string)), rows: 3);
        var tokens = new int[reader.FieldCount];
        var state = factory.Tokenize(reader, tokens, 0);

        // one Tokenize per column, before any row is read
        Assert.Equal(1, handler.TokenizeCount);

        var seen = 0;
        while (reader.Read())
        {
            var row = factory.Read(reader, tokens, 0, state);
            Assert.Equal(2000 + seen, row.Day.Year); // the token chose the string path
            seen++;
        }

        Assert.Equal(3, seen);
        Assert.Equal(3, handler.ParseCount); // once per row...
        Assert.Equal(1, handler.TokenizeCount); // ...but still only one Tokenize
        Assert.All(handler.TokensSeen, token => Assert.Equal(StringShaped, token));
    }

    [Fact]
    public void TokenizeSeesTheRightColumn_WhenTheFactoryStartsPartWayAlong()
    {
        // multi-column readers hand a factory a slice: the handler must be asked about *its*
        // column, which is what the generated `handlerOffset + i` exists to get right
        var handler = new CountingHandler();
        var factory = new HandRolledFactory(handler);

        using var reader = CreateReader(("Ignored", typeof(int)), ("Day", typeof(string)), rows: 1);
        var tokens = new int[1];
        var state = factory.Tokenize(reader, tokens, columnOffset: 1);

        Assert.Equal(1, handler.TokenizeCount);
        Assert.Equal(1, handler.TokenizedColumn); // not column 0
        Assert.True(reader.Read());
        Assert.Equal(2000, factory.Read(reader, tokens, 1, state).Day.Year);
    }

    private const int StringShaped = 1;

    private static DataTableReader CreateReader(params (string Name, Type Type)[] columns)
        => CreateReader(1, columns);

    private static DataTableReader CreateReader((string Name, Type Type) column, int rows)
        => CreateReader(rows, column);

    private static DataTableReader CreateReader((string Name, Type Type) a, (string Name, Type Type) b, int rows)
        => CreateReader(rows, a, b);

    private static DataTableReader CreateReader(int rows, params (string Name, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (name, type) in columns) table.Columns.Add(name, type);
        for (int r = 0; r < rows; r++)
        {
            var values = new object[columns.Length];
            for (int c = 0; c < columns.Length; c++)
            {
                values[c] = columns[c].Type == typeof(string) ? $"{2000 + r}-01-01" : r;
            }
            table.Rows.Add(values);
        }
        return table.CreateDataReader();
    }

    public struct LocalDate { public int Year { get; set; } }
    public class Appointment { public LocalDate Day { get; set; } }

    private sealed class CountingHandler : DbValueHandler<LocalDate>
    {
        public int TokenizeCount { get; private set; }
        public int ParseCount { get; private set; }
        public int TokenizedColumn { get; private set; } = -1;
        public System.Collections.Generic.List<int> TokensSeen { get; } = [];

        protected override void SetValueCore(DbParameter parameter, LocalDate value)
            => parameter.Value = value.Year;

        public override int Tokenize(DbDataReader reader, int columnOffset)
        {
            TokenizeCount++;
            TokenizedColumn = columnOffset;
            return reader.GetFieldType(columnOffset) == typeof(string) ? StringShaped : 0;
        }

        public override LocalDate Parse(DbDataReader reader, int ordinal, int token)
        {
            ParseCount++;
            TokensSeen.Add(token);
            // the whole point: the per-column decision is not re-made here
            return token == StringShaped
                ? new LocalDate { Year = int.Parse(reader.GetString(ordinal).Substring(0, 4)) }
                : new LocalDate { Year = reader.GetInt32(ordinal) };
        }

        protected override LocalDate Parse(object? value) => throw new NotSupportedException();
    }

    /// <summary>Written the way <c>WriteRowFactory</c> emits for a handler-bound member.</summary>
    private sealed class HandRolledFactory(CountingHandler handler) : RowFactory<Appointment>
    {
        public override object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset)
        {
            var handlerTokens = new int[tokens.Length];
            var handlerOffset = columnOffset;
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = 0; // "Day", via the handler
                columnOffset++;
            }
            for (int i = 0; i < tokens.Length; i++)
            {
                switch (tokens[i])
                {
                    case 0:
                        handlerTokens[i] = handler.Tokenize(reader, handlerOffset + i);
                        break;
                }
            }
            return handlerTokens;
        }

        public override Appointment Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
        {
            Appointment result = new();
            var handlerTokens = (int[])state!;
            for (int i = 0; i < tokens.Length; i++)
            {
                switch (tokens[i])
                {
                    case 0:
                        result.Day = handler.Parse(reader, columnOffset, handlerTokens[i]);
                        break;
                }
                columnOffset++;
            }
            return result;
        }
    }
}

using Dapper;
using System;
using System.Data;
using Xunit;

namespace Dapper.AOT.Test;

public class RowFactoryReaderTests
{
    private static DataTableReader CreateReader()
    {
        var table = new DataTable();
        table.Columns.Add("Value", typeof(int));
        table.Rows.Add(42);
        return table.CreateDataReader();
    }

    [Fact]
    public void GetRowParser_IDataReader_WorksForDbDataReader()
    {
        using var reader = CreateReader();
        Assert.True(reader.Read());
        var parser = RowFactory.Inbuilt.Value<int>().GetRowParser((IDataReader)reader);
        Assert.Equal(42, parser(reader));
    }

    [Fact]
    public void GetRowParser_IDataReader_ThrowsForNonDbDataReader()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => RowFactory.Inbuilt.Value<int>().GetRowParser(new NotADbDataReader()));
        Assert.Equal("reader", ex.ParamName);
    }

    private sealed class NotADbDataReader : IDataReader
    {
        public object this[int i] => throw new NotSupportedException();
        public object this[string name] => throw new NotSupportedException();
        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => 0;
        public int FieldCount => 0;
        public void Close() { }
        public void Dispose() { }
        public bool GetBoolean(int i) => throw new NotSupportedException();
        public byte GetByte(int i) => throw new NotSupportedException();
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public char GetChar(int i) => throw new NotSupportedException();
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
        public IDataReader GetData(int i) => throw new NotSupportedException();
        public string GetDataTypeName(int i) => throw new NotSupportedException();
        public DateTime GetDateTime(int i) => throw new NotSupportedException();
        public decimal GetDecimal(int i) => throw new NotSupportedException();
        public double GetDouble(int i) => throw new NotSupportedException();
        public Type GetFieldType(int i) => throw new NotSupportedException();
        public float GetFloat(int i) => throw new NotSupportedException();
        public Guid GetGuid(int i) => throw new NotSupportedException();
        public short GetInt16(int i) => throw new NotSupportedException();
        public int GetInt32(int i) => throw new NotSupportedException();
        public long GetInt64(int i) => throw new NotSupportedException();
        public string GetName(int i) => throw new NotSupportedException();
        public int GetOrdinal(string name) => throw new NotSupportedException();
        public DataTable? GetSchemaTable() => null;
        public string GetString(int i) => throw new NotSupportedException();
        public object GetValue(int i) => throw new NotSupportedException();
        public int GetValues(object[] values) => throw new NotSupportedException();
        public bool IsDBNull(int i) => throw new NotSupportedException();
        public bool NextResult() => false;
        public bool Read() => false;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Dapper.Internal;

internal sealed class DynamicRecordReader<T> : RowFactory<T> where T : class
{
    // it is the callers job to make sure that DynamicRecord : T - that's not something that can be done via constraints

    public static readonly DynamicRecordReader<T> Instance = new();
    private DynamicRecordReader() { }
    public override object? Tokenize(DbDataReader reader, Span<int> tokens, int columnOffset)
    {
        var arr = tokens.IsEmpty ? Array.Empty<DynamicRecordField>() : new DynamicRecordField[tokens.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = new DynamicRecordField(reader.GetName(columnOffset), reader.GetFieldType(columnOffset), reader.GetDataTypeName(columnOffset));
            columnOffset++;
        }
        return arr;
    }

    public override T Read(DbDataReader reader, ReadOnlySpan<int> tokens, int columnOffset, object? state)
        => (T)(object)new DynamicRecord((DynamicRecordField[])state!, reader, columnOffset);
}
internal readonly struct DynamicRecordField
{
    public DynamicRecordField(string name, Type type, string dataTypeName)
    {
        Name = name;
        Type = type;
        DataTypeName = dataTypeName;
    }
    public readonly Type Type;
    public readonly string Name, DataTypeName;
}
internal sealed class DynamicRecord : DbDataRecord, IReadOnlyDictionary<string, object?>, IDictionary<string, object?>,
    IDynamicMetaObjectProvider
{
    // the fields array is shared between every record of the shape (it is the Tokenize
    // state), so structural mutation (add/remove) must copy-on-write before diverging
    private DynamicRecordField[] fields;
    private object?[] values;
    private string[]? namesCopy;
    public DynamicRecord(DynamicRecordField[] fields, DbDataReader source, int columnOffset)
    {
        this.fields = fields;
        if (fields.Length == 0)
        {
            values = Array.Empty<object>();
        }
        else
        {
            var typed = new object[fields.Length];
            if (columnOffset == 0 && source.FieldCount == typed.Length)
            {
                source.GetValues(typed);
            }
            else
            {
                for (int i = 0; i < typed.Length; i++)
                {
                    typed[i] = source.GetValue(columnOffset++);
                }
            }
            values = typed;
            for (int i = 0; i < values.Length; i++)
            {
                // the dictionary and dynamic surfaces hand back null, not DBNull, matching
                // vanilla Dapper's DapperRow; the DbDataRecord surface restores DBNull
                if (values[i] is DBNull) values[i] = null;
            }
        }
    }

    private object? GetRawValue(string name)
    {
        // try-get semantics: vanilla's DapperRow returns null for a missing member (a
        // value-type dynamic cast is then what produces the RuntimeBinderException)
        var index = GetOrdinal(name);
        return index < 0 ? null : values[index];
    }

    private void SetValue(string name, object? value)
    {
        var i = GetOrdinal(name);
        if (i >= 0)
        {
            values[i] = value;
            return;
        }
        // grow; the fields array may still be the shared per-shape one, so always copy
        var len = fields.Length;
        var newFields = new DynamicRecordField[len + 1];
        Array.Copy(fields, newFields, len);
        newFields[len] = new DynamicRecordField(name, value?.GetType() ?? typeof(object), "");
        var newValues = new object?[len + 1];
        Array.Copy(values, newValues, len);
        newValues[len] = value;
        fields = newFields;
        values = newValues;
        namesCopy = null;
    }

    private bool Remove(string name)
    {
        var i = GetOrdinal(name);
        if (i < 0) return false;
        var len = fields.Length;
        var newFields = new DynamicRecordField[len - 1];
        Array.Copy(fields, newFields, i);
        Array.Copy(fields, i + 1, newFields, i, len - i - 1);
        var newValues = new object?[len - 1];
        Array.Copy(values, newValues, i);
        Array.Copy(values, i + 1, newValues, i, len - i - 1);
        fields = newFields;
        values = newValues;
        namesCopy = null;
        return true;
    }
    public override int GetOrdinal(string name)
    {
        var fields = this.fields;
        for (int i = 0; i < fields.Length; i++)
        {
            if (name == fields[i].Name) return i;
        }
        for (int i = 0; i < fields.Length; i++)
        {
            if (string.Equals(name, fields[i].Name, StringComparison.InvariantCultureIgnoreCase)) return i;
        }
        return -1;
    }
    public override int GetValues(object[] values)
    {
        var count = Math.Max(values.Length, FieldCount);
        Array.Copy(this.values, values, count);
        return count;
    }

    public override int FieldCount => fields.Length;

    public IEnumerable<string> Keys => GetSafeNamesCopy();

    public IEnumerable<object?> Values => values;

    public int Count => FieldCount;

    ICollection<string> IDictionary<string, object?>.Keys => GetSafeNamesCopy();

    private string[] GetSafeNamesCopy()
    {
        if (namesCopy is null)
        {
            var len = fields.Length;
            if (len == 0) return Array.Empty<string>();
            var arr = new string[len];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = fields[i].Name;
            }
            namesCopy = arr;
        }
        return namesCopy;
    }

    ICollection<object?> IDictionary<string, object?>.Values => values;

    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    object? IDictionary<string, object?>.this[string key]
    {
        get => GetRawValue(key);
        set => SetValue(key, value); // set-or-add, matching vanilla's DapperRow
    }

    object? IReadOnlyDictionary<string, object?>.this[string key] => GetRawValue(key);

    public override string GetName(int i) => fields[i].Name;
    public override Type GetFieldType(int i) => fields[i].Type;
    protected override DbDataReader GetDbDataReader(int i) => throw new NotSupportedException();

    public override object GetValue(int i) => values[i] ?? DBNull.Value;
    public override object this[string name]
    {
        get
        {
            var index = GetOrdinal(name);
            if (index < 0) Throw(name);
            return values[index] ?? DBNull.Value;

            static void Throw(string name) => throw new KeyNotFoundException($"Member '{name}' not found");
        }
    }
    public override object this[int i] => values[i] ?? DBNull.Value;

    private T As<T>(int i) => CommandUtils.As<T>(values[i]);
    public override bool GetBoolean(int i) => As<bool>(i);
    public override char GetChar(int i) => As<char>(i);
    public override string GetString(int i) => As<string>(i);
    public override byte GetByte(int i) => As<byte>(i);
    public override DateTime GetDateTime(int i) => As<DateTime>(i);
    public override decimal GetDecimal(int i) => As<decimal>(i);
    public override double GetDouble(int i) => As<double>(i);
    public override float GetFloat(int i) => As<float>(i);
    public override Guid GetGuid(int i) => As<Guid>(i);
    public override short GetInt16(int i) => As<short>(i);
    public override int GetInt32(int i) => As<int>(i);
    public override long GetInt64(int i) => As<long>(i);
    public override bool IsDBNull(int i) => values[i] is DBNull or null;
    public override string GetDataTypeName(int i) => fields[i].DataTypeName;
    static int CheckOffsetAndComputeLength(int totalLength, long dataIndex, ref int length)
    {
        var offset = checked((int)dataIndex);
        var remaining = totalLength - offset;
#if NETCOREAPP3_1_OR_GREATER
        length = Math.Clamp(remaining, 0, length);
#else
    length = Math.Max(Math.Min(remaining, length), 0);
#endif
        return offset;
    }
    public override long GetBytes(int i, long dataIndex, byte[]? buffer, int bufferIndex, int length)
    {
        if (buffer is null) return 0;
        byte[] blob = (byte[])GetValue(i);
        Buffer.BlockCopy(blob, CheckOffsetAndComputeLength(blob.Length, dataIndex, ref length), buffer, bufferIndex, length);
        return length;
    }
    public override long GetChars(int i, long dataIndex, char[]? buffer, int bufferIndex, int length)
    {
        if (buffer is null) return 0;
        if (values[i] is string s)
        {
            s.CopyTo(CheckOffsetAndComputeLength(s.Length, dataIndex, ref length), buffer, bufferIndex, length);
        }
        else
        {
            char[] clob = (char[])GetValue(i);
            Array.Copy(clob, CheckOffsetAndComputeLength(clob.Length, dataIndex, ref length), buffer, bufferIndex, length);
        }
        return length;
    }

    public bool ContainsKey(string key) => GetOrdinal(key) >= 0;

    public bool TryGetValue(string key, out object? value)
    {
        var i = GetOrdinal(key);
        if (i >= 0)
        {
            value = values[i];
            return true;
        }
        value = null;
        return false;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (int i = 0; i < FieldCount; i++)
        {
            yield return new(fields[i].Name, values[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void IDictionary<string, object?>.Add(string key, object? value) => SetValue(key, value);

    bool IDictionary<string, object?>.Remove(string key) => Remove(key);

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => SetValue(item.Key, item.Value);

    void ICollection<KeyValuePair<string, object?>>.Clear()
    {
        fields = Array.Empty<DynamicRecordField>();
        values = Array.Empty<object>();
        namesCopy = null;
    }

    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
        => TryGetValue(item.Key, out var value) && Equals(value, item.Value);

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        for (int i = 0; i < FieldCount; i++)
        {
            array[arrayIndex++] = new(fields[i].Name, values[i]);
        }
    }

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
        => TryGetValue(item.Key, out var value) && Equals(value, item.Value) && Remove(item.Key);

    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        => new DynamicRecordMetaObject(parameter, BindingRestrictions.Empty, this);

    private sealed class DynamicRecordMetaObject : DynamicMetaObject
    {
        private static readonly MethodInfo getValueMethod, setValueMethod;
        static DynamicRecordMetaObject()
        {
            IDictionary<string, object> tmp = new Dictionary<string, object> { { "", "" } };
            _ = tmp[""]; // to ensure the indexers are not trimmed away
            tmp[""] = "";
            getValueMethod = typeof(IReadOnlyDictionary<string, object>).GetProperty("Item")?.GetGetMethod()
                ?? throw new InvalidOperationException("Unable to resolve indexer");
            setValueMethod = typeof(IDictionary<string, object>).GetProperty("Item")?.GetSetMethod()
                ?? throw new InvalidOperationException("Unable to resolve indexer");
        }

        public DynamicRecordMetaObject(
            Expression expression,
            BindingRestrictions restrictions,
            object value
            )
            : base(expression, restrictions, value)
        {
        }

        private DynamicMetaObject CallMethod(
            MethodInfo method,
            Expression[] parameters
            )
        {
            var callMethod = new DynamicMetaObject(
                Expression.Call(
                    Expression.Convert(Expression, LimitType),
                    method,
                    parameters),
                BindingRestrictions.GetTypeRestriction(Expression, LimitType)
                );
            return callMethod;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var parameters = new Expression[] { Expression.Constant(binder.Name) };

            var callMethod = CallMethod(getValueMethod, parameters);

            return callMethod;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            // the binding must *produce* the assigned value (assignment is an expression);
            // the indexer's set returns void, so wrap it in a block yielding the value
            var valueArg = Expression.Convert(value.Expression, typeof(object));
            var call = Expression.Call(
                Expression.Convert(Expression, LimitType),
                setValueMethod,
                Expression.Constant(binder.Name),
                valueArg);
            return new DynamicMetaObject(
                Expression.Block(typeof(object), call, valueArg),
                BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        // Needed for Visual basic dynamic support
        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            var parameters = new Expression[]
            {
                Expression.Constant(binder.Name)
            };

            var callMethod = CallMethod(getValueMethod, parameters);

            return callMethod;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            if (HasValue && Value is IDictionary<string, object> lookup) return lookup.Keys;
            return Array.Empty<string>();
        }
    }
}
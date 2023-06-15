//using Dapper.Internal;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Data;
//using System.Data.Common;
//using System.IO;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;
//using static Dapper.ObjectReader;

//namespace Dapper;

//// user code
//partial class SomeType
//{
//    /* ... members ...*/
//    public int Foo { get; set; }
//    public string Bar { get; set; }
//}

//// generated
//partial class SomeType
//{
//    public static DbDataReader CreateDbDataReader(IEnumerable<SomeType> source)
//        => new MyObjectReader(source, MyObjectReader.DefaultColumnNames);
//    public static DbDataReader CreateDbDataReader(IEnumerable<SomeType> source, params string[] columnNames)
//        => new MyObjectReader(source, columnNames);
//}
//// generated
//sealed file class MyObjectReader : ObjectReader<SomeType>
//{
//    internal static readonly string[] DefaultColumnNames = new string[] { nameof(SomeType.Foo), nameof(SomeType.Bar) };
//    internal MyObjectReader(IEnumerable<SomeType> source, string[] columnNames)
//        : base(source, columnNames) { }

//    protected override Type TokenizedGetType(int token) => token switch
//    {
//        0 => typeof(int),
//        1 => typeof(string),
//        _ => base.TokenizedGetType(token)
//    };
//    protected override object TokenizedGetValue(int token, SomeType value)
//    => token switch
//    {
//        0 => value.Foo,
//        1 => value.Bar,
//        _ => base.TokenizedGetValue(token, value)
//    };
//    protected override TValue TokenizedGetValue<TValue>(int token, SomeType value)
//    {
//        switch (token)
//        {
//            case 0 when typeof(TValue) == typeof(int):
//                return UnsafeAs<int, TValue>(value.Foo);
//            case 1 when typeof(TValue) == typeof(string):
//                return UnsafeAs<string, TValue>(value.Bar);
//            default:
//                return base.TokenizedGetValue<TValue>(token, value);
//        }
//    }
//    protected override bool TokenizedIsNull(int token, SomeType value) => token switch
//    {
//        0 => false,
//        1 => value.Bar is null,
//        _ => base.TokenizedIsNull(token, value),
//    };
//    protected override bool TryTokenize(string name, out int token)
//    {
//        switch (name)
//        {
//            case nameof(SomeType.Foo):
//                token = 0;
//                return true;
//            case nameof(SomeType.Bar):
//                token = 1;
//                return true;
//            default:
//                return base.TryTokenize(name, out token);
//        }
//    }
//}

//// library code (zero testing here)

///// <summary>
///// Acts as a <see cref="DbDataReader"/> over object data
///// </summary>
//public abstract class ObjectReader<T> : DbDataReader, IEnumerator, IDbColumnSchemaGenerator
//{
//    private State _state;
//    private ColumnMetadata[] _fields;
//    private readonly IEnumerator<T> source;

//    /// <summary>
//    /// Create a new object reader
//    /// </summary>
//    public ObjectReader(IEnumerable<T> source, params string[] columnNames)
//    {
//        var fields = _fields = columnNames is null || columnNames.Length == 0
//            ? Array.Empty<ColumnMetadata>()
//            : new ColumnMetadata[columnNames.Length];

//        for (int i = 0; i < fields.Length; i++)
//        {
//            var name = columnNames![i];
//            if (!TryTokenize(name, out int token))
//            {
//                ThrowColumnNotFound(name);
//            }
//            fields[i] = new ColumnMetadata(token, name, TokenizedGetType(token, out bool isNullable), isNullable);
//        }
//        this.source = source.GetEnumerator();
//        _state = this.source.MoveNext() ? State.Bof : State.Empty;
//    }

//    /// <inheritdoc/>
//    public sealed override int VisibleFieldCount => FieldCount;

//    /// <inheritdoc/>
//    public sealed override bool Read()
//    {
//        switch (_state)
//        {
//            case State.Bof:
//                _state = State.Data;
//                return true;
//            case State.Data:
//                if (source.MoveNext())
//                {
//                    return true;
//                }
//                else
//                {
//                    _state = State.Eof;
//                    return false;
//                }
//            case State.Empty:
//            case State.Eof:
//            case State.Closed:
//            default:
//                return false;
//        }
//    }
//    /// <inheritdoc/>
//    public sealed override bool NextResult()
//    {
//        switch (_state)
//        {
//            case State.Bof:
//            case State.Data:
//                _state = State.Eof;
//                break;
//        }
//        return false;
//    }

//    /// <inheritdoc/>
//    public sealed override bool IsClosed => _state == State.Closed;
//    /// <inheritdoc/>
//    public sealed override void Close() => Dispose(true);

//    /// <inheritdoc/>
//    public sealed override bool HasRows => _state != State.Empty;

//    private T Current => _state == State.Data ? source.Current : default!;
//    /// <summary>
//    /// Resolve a column by name into a semantic token
//    /// </summary>
//    protected virtual bool TryTokenize(string name, out int token)
//    {
//        token = -1;
//        return false;
//    }
//    /// <summary>
//    /// Determine whether a value is null via the token
//    /// </summary>
//    protected virtual bool TokenizedIsNull(int token, T value) => throw new ArgumentOutOfRangeException(nameof(token));
//    /// <summary>
//    /// Get a value via the token
//    /// </summary>
//    protected virtual TValue TokenizedGetValue<TValue>(int token, T value)
//        => CommandUtils.As<TValue>(TokenizedGetValue(token, value));

//    /// <summary>
//    /// Get the type of a column via the token
//    /// </summary>
//    protected virtual Type TokenizedGetType(int token, out bool isNullable) => throw new ArgumentOutOfRangeException(nameof(token));
//    /// <summary>
//    /// Get a value via the token
//    /// </summary>
//    protected virtual object TokenizedGetValue(int token, T value) => throw new ArgumentOutOfRangeException(nameof(token));

//    /// <inheritdoc/>
//    public sealed override int GetOrdinal(string name)
//    {
//        var fields = _fields;
//        for (int i = 0; i < fields.Length; i++)
//        {
//            if (string.Equals(fields[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
//            {
//                return i;
//            }
//        }
//        ObjectReader.ThrowColumnNotFound(name);
//        return -1;
//    }

//    private int GetToken(string name)
//    {
//        var fields = _fields;
//        for (int i = 0; i < fields.Length; i++)
//        {
//            if (string.Equals(fields[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
//            {
//                return fields[i].Token;
//            }
//        }
//        ObjectReader.ThrowColumnNotFound(name);
//        return -1;
//    }

//    private int GetToken(int ordinal) => _fields[ordinal].Token;

//    /// <inheritdoc/>
//    public sealed override int FieldCount => _fields.Length;

//    /// <inheritdoc/>
//    protected sealed override void Dispose(bool disposing)
//    {
//        if (disposing)
//        {
//            _state = State.Closed;
//            source.Dispose();
//        }
//        base.Dispose(disposing);
//    }
//    /// <inheritdoc/>
//    public sealed override object GetValue(int ordinal) => TokenizedGetValue(GetToken(ordinal), Current) ?? DBNull.Value;
//    /// <inheritdoc/>
//    public sealed override object this[int ordinal] => TokenizedGetValue(GetToken(ordinal), Current) ?? DBNull.Value;
//    /// <inheritdoc/>
//    public sealed override object this[string name] => TokenizedGetValue(GetToken(name), Current) ?? DBNull.Value;
//    /// <inheritdoc/>
//    public sealed override Type GetFieldType(int ordinal) => _fields[ordinal].Type;
//    /// <inheritdoc/>
//    public sealed override bool IsDBNull(int ordinal) => TokenizedIsNull(GetToken(ordinal), Current);
//    /// <inheritdoc/>
//    public sealed override string GetString(int ordinal) => GetFieldValue<string>(ordinal);
//    /// <inheritdoc/>
//    public sealed override TextReader GetTextReader(int ordinal) => new StringReader(GetString(ordinal));
//    /// <inheritdoc/>
//    public sealed override bool GetBoolean(int ordinal) => GetFieldValue<bool>(ordinal);
//    /// <inheritdoc/>
//    public sealed override byte GetByte(int ordinal) => GetFieldValue<byte>(ordinal);
//    /// <inheritdoc/>
//    public sealed override char GetChar(int ordinal) => GetFieldValue<char>(ordinal);
//    /// <inheritdoc/>
//    public sealed override double GetDouble(int ordinal) => GetFieldValue<double>(ordinal);
//    /// <inheritdoc/>
//    public sealed override float GetFloat(int ordinal) => GetFieldValue<float>(ordinal);
//    /// <inheritdoc/>
//    public sealed override short GetInt16(int ordinal) => GetFieldValue<short>(ordinal);
//    /// <inheritdoc/>
//    public sealed override int GetInt32(int ordinal) => GetFieldValue<int>(ordinal);
//    /// <inheritdoc/>
//    public sealed override long GetInt64(int ordinal) => GetFieldValue<long>(ordinal);
//    /// <inheritdoc/>
//    public sealed override Guid GetGuid(int ordinal) => GetFieldValue<Guid>(ordinal);
//    /// <inheritdoc/>
//    public sealed override decimal GetDecimal(int ordinal) => GetFieldValue<decimal>(ordinal);
//    /// <inheritdoc/>
//    public sealed override DateTime GetDateTime(int ordinal) => GetFieldValue<DateTime>(ordinal);
//    /// <inheritdoc/>
//    public sealed override int Depth => 0;
//    /// <inheritdoc/>
//    public sealed override int RecordsAffected => 0;

//    /// <inheritdoc/>
//    public sealed override string GetName(int ordinal) => columnNames[ordinal];
//    /// <inheritdoc/>
//    public sealed override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
//    /// <inheritdoc/>
//    protected sealed override DbDataReader GetDbDataReader(int ordinal) => throw new NotSupportedException();
//    /// <inheritdoc/>
//    public sealed override int GetValues(object[] values)
//    {
//        var tokens = Tokens;
//        var current = Current;
//        for (int i = 0; i < tokens.Length; i++)
//        {
//            values[i] = TokenizedGetValue(tokens[i], current);
//        }
//        return tokens.Length;
//    }

//    /// <inheritdoc/>
//    public sealed override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
//    {
//        var raw = GetFieldValue<byte[]>(ordinal);
//        int count = checked((int)(raw.Length - dataOffset));
//        if (count < 0) count = 0;
//        else if (count > length) count = length;
//        if (count != 0)
//        {
//            Buffer.BlockCopy(raw, checked((int)dataOffset), buffer!, bufferOffset, count);
//        }
//        return count;
//    }
//    /// <inheritdoc/>
//    public sealed override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
//    {
//        var raw = GetFieldValue<string>(ordinal);
//        int count = checked((int)(raw.Length - dataOffset));
//        if (count < 0) count = 0;
//        else if (count > length) count = length;
//        if (count != 0)
//        {
//            raw.CopyTo(checked((int)dataOffset), buffer!, bufferOffset, count);
//        }
//        return count;
//    }
//    /// <inheritdoc/>
//    public sealed override Stream GetStream(int ordinal) => new MemoryStream(GetFieldValue<byte[]>(ordinal));

//    object IEnumerator.Current => this;
//    bool IEnumerator.MoveNext() => Read();
//    void IEnumerator.Reset() => throw new NotSupportedException();

//    /// <inheritdoc/>
//    public sealed override IEnumerator GetEnumerator() => this;
//    /// <inheritdoc/>
//    public sealed override TValue GetFieldValue<TValue>(int ordinal) => TokenizedGetValue<TValue>(GetToken(ordinal), Current);
//    /// <inheritdoc/>
//    public sealed override Type GetProviderSpecificFieldType(int ordinal) => GetFieldType(ordinal);
//    /// <inheritdoc/>
//    public sealed override object GetProviderSpecificValue(int ordinal) => GetValue(ordinal);
//    /// <inheritdoc/>
//    public sealed override int GetProviderSpecificValues(object[] values) => GetValues(values);
//#if NETCOREAPP3_1_OR_GREATER
//    /// <inheritdoc/>
//    public sealed override Task CloseAsync()
//    {
//        Close();
//        return Task.CompletedTask;
//    }
//    /// <inheritdoc/>
//    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Via Dispose")]
//    public sealed override ValueTask DisposeAsync()
//    {
//        Dispose();
//        return default;
//    }
//    /// <inheritdoc/>
//    public sealed override Task<TValue> GetFieldValueAsync<TValue>(int ordinal, CancellationToken cancellationToken)
//        => Task.FromResult(GetFieldValue<TValue>(ordinal));
//    /// <inheritdoc/>
//    public sealed override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
//        => IsDBNull(ordinal) ? ObjectReader.True : ObjectReader.False;
//    /// <inheritdoc/>
//    public sealed override Task<bool> NextResultAsync(CancellationToken cancellationToken)
//        => NextResult() ? ObjectReader.True : ObjectReader.False;
//    /// <inheritdoc/>
//    public sealed override Task<bool> ReadAsync(CancellationToken cancellationToken)
//        => Read() ? ObjectReader.True : ObjectReader.False;
//#endif
//#if NET6_0_OR_GREATER
//    /// <inheritdoc/>
//    public sealed override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default)
//        => Task.FromResult(GetSchemaTable());
//    /// <inheritdoc/>
//    public sealed override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default)
//        => Task.FromResult(GetColumnSchema());
//#endif
//    /// <inheritdoc cref="IDbColumnSchemaGenerator.GetColumnSchema"/>
//    public abstract ReadOnlyCollection<DbColumn> GetColumnSchema();

//    /// <inheritdoc/>
//#if NET6_0_OR_GREATER
//    [Obsolete("This API is not supported", true)]
//#endif
//    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
//    public sealed override object InitializeLifetimeService() => throw new NotSupportedException();

//    /// <inheritdoc/>
//    public sealed override DataTable? GetSchemaTable()
//    {
//        // these are the columns used by DataTable load
//        DataTable table = new DataTable
//        {
//            Columns =
//            {
//                {"ColumnOrdinal", typeof(int)},
//                {"ColumnName", typeof(string)},
//                {"DataType", typeof(Type)},
//                {"ColumnSize", typeof(int)},
//                {"AllowDBNull", typeof(bool)}
//            }
//        };
//        object[] rowData = new object[5];
//        var tokens = Tokens;
//        for (int i = 0; i < FieldCount; i++)
//        {
//            var token = tokens[i];
//            rowData[0] = i;
//            rowData[1] = columnNames[i];
//            rowData[2] = GetFieldType(i);
//            rowData[3] = -1;
//            rowData[4] = ;
//            table.Rows.Add(rowData);
//        }
//        return table;
//    }

//    /// <inheritdoc/>
//    public sealed override int GetHashCode() => base.GetHashCode();
//    /// <inheritdoc/>
//    public sealed override bool Equals(object? obj) => base.Equals(obj);
//    /// <inheritdoc/>
//    public sealed override string ToString() => nameof(ObjectReader);
//    /// <summary>
//    /// Reinterpret a value
//    /// </summary>
//    protected TTo UnsafeAs<TFrom, TTo>(TFrom value) => Unsafe.As<TFrom, TTo>(ref value);
//}

//internal static class ObjectReader
//{
//    public static readonly Task<bool> True = Task.FromResult(true), False = Task.FromResult(false);

//    internal static void ThrowColumnNotFound(string name) => throw new KeyNotFoundException("Column not found: " + name);

//    internal enum State
//    {
//        Empty, Bof, Data, Eof, Closed
//    }
//    internal readonly struct ColumnMetadata
//    {
//        public int Token { get; }
//        public bool IsNullable { get; }
//        public string Name { get; }
//        public Type Type { get; }

//        public ColumnMetadata(int token, string name, Type type, bool isNullable)
//        {
//            Token = token;
//            Name = name;
//            Type = type;
//            IsNullable = isNullable;
//        }
//    }
//}
